using Blazor.Extensions;
using Blazor.Extensions.Canvas.WebGL;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Threading.Tasks;

namespace BlazorWebGLExamples.Example2.Pages
{
    public class IndexBase : ComponentBase
    {
        private WebGLContext _gl;

        protected BECanvasComponent _canvasReference;

        [Inject] ILogger<IndexBase> Logger { get; set; }

        private readonly string vsSource =
        @"attribute vec4 aVertexPosition;

        uniform mat4 uModelViewMatrix;
        uniform mat4 uProjectionMatrix;

        void main() {
            gl_Position = uProjectionMatrix * uModelViewMatrix * aVertexPosition;
        }";

        private readonly string fsSource =
        @"void main() {
        gl_FragColor = vec4(1.0, 1.0, 1.0, 1.0);
        }";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            _gl = await _canvasReference.CreateWebGLAsync();

            var shaderProgram = await InitShaderProgram(_gl, vsSource, fsSource);
            var buffers = await InitBuffers(_gl);
            await DrawScene(_gl, shaderProgram, buffers);
        }

        private async Task<WebGLProgram> InitShaderProgram(WebGLContext gl, string vsSource, string fsSource)
        {
            var vertexShader = await LoadShader(gl, ShaderType.VERTEX_SHADER, vsSource);
            var fragmentShader = await LoadShader(gl, ShaderType.FRAGMENT_SHADER, fsSource);

            // Create the shader program
            var shaderProgram = await gl.CreateProgramAsync();
            await gl.AttachShaderAsync(shaderProgram, vertexShader);
            await gl.AttachShaderAsync(shaderProgram, fragmentShader);
            await gl.LinkProgramAsync(shaderProgram);

            // If creating the shader program failed, alert
            if (!await gl.GetProgramParameterAsync<bool>(shaderProgram, ProgramParameter.LINK_STATUS))
            {
                var infoLog = await gl.GetProgramInfoLogAsync(shaderProgram);
                Logger.LogError($"Unable to initialize the shader program: {infoLog }");
                return null;
            }

            return shaderProgram;
        }

        private async Task<WebGLShader> LoadShader(WebGLContext gl, ShaderType type, string source)
        {
            var shader = await gl.CreateShaderAsync(type);
            await gl.ShaderSourceAsync(shader, source);
            await gl.CompileShaderAsync(shader);
            if (!await gl.GetShaderParameterAsync<bool>(shader, ShaderParameter.COMPILE_STATUS))
            {
                var infoLog = await gl.GetShaderInfoLogAsync(shader);
                Logger.LogError($"An error occurred compiling the shaders: {infoLog }");
                await gl.DeleteShaderAsync(shader);
                return null;
            }

            return shader;
        }

        private async Task<WebGLBuffer> InitBuffers(WebGLContext gl)
        {
            // Create a buffer for the square's positions.
            var positionBuffer = await gl.CreateBufferAsync();

            // Select the positionBuffer as the one to apply buffer
            // operations to from here out.
            await gl.BindBufferAsync(BufferType.ARRAY_BUFFER, positionBuffer);

            // Now create an array of positions for the square.
            var positions = new float[] {
          -1.0f, 1.0f,
           1.0f, 1.0f,
          -1.0f, -1.0f,
           1.0f, -1.0f,
        };

            // Now pass the list of positions into WebGL to build the
            // shape. We do this by creating a Float32Array from the
            // JavaScript array, then use it to fill the current buffer.
            await gl.BufferDataAsync(BufferType.ARRAY_BUFFER,
                          positions,
                          BufferUsageHint.STATIC_DRAW);

            return positionBuffer;
        }

        private float[] ToArray(Matrix4x4 matrix)
        {
            return new float[16]
            {
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44,
            };
        }

        private async Task DrawScene(WebGLContext gl, WebGLProgram program, WebGLBuffer buffer)
        {
            await gl.ClearColorAsync(0.0f, 0.0f, 0.0f, 1.0f);   // Clear to black, fully opaque
            await gl.ClearDepthAsync(1.0f);                     // Clear everything
            await gl.EnableAsync(EnableCap.DEPTH_TEST);         // Enable depth testing
            await gl.DepthFuncAsync(CompareFunction.LEQUAL);    // Near things obscure far things

            // Clear the canvas before we start drawing on it.
            await gl.ClearAsync(BufferBits.COLOR_BUFFER_BIT | BufferBits.DEPTH_BUFFER_BIT);
            await gl.BindBufferAsync(BufferType.ARRAY_BUFFER, buffer);

            var attribute = await gl.GetAttribLocationAsync(program, "aVertexPosition");
            await gl.VertexAttribPointerAsync((uint)attribute, 2, DataType.FLOAT, false, 0, 0);
            await gl.EnableVertexAttribArrayAsync((uint)attribute);

            // Tell WebGL to use our program when drawing
            await gl.UseProgramAsync(program);

            var width = _canvasReference.Width / 10;
            var height = _canvasReference.Height / 10;

            var projectionMatrix = Matrix4x4.CreateOrthographic(width, height, 0.1f, 100.0f);
            var modelViewMatrix = Matrix4x4.CreateTranslation(new Vector3(-0.0f, 0.1f, -6.0f));

            // Set the shader uniforms
            await gl.UniformMatrixAsync(await gl.GetUniformLocationAsync(program, "uProjectionMatrix"), false, ToArray(projectionMatrix));
            await gl.UniformMatrixAsync(await gl.GetUniformLocationAsync(program, "uModelViewMatrix"), false, ToArray(modelViewMatrix));

            await gl.DrawArraysAsync(Primitive.TRINAGLE_STRIP, 0, 4);
        }
    }
}
