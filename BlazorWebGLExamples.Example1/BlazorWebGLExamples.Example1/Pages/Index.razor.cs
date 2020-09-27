using Blazor.Extensions;
using Blazor.Extensions.Canvas.WebGL;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace BlazorWebGLExamples.Example1.Pages
{
    public class IndexBase : ComponentBase
    {
        private WebGLContext _context;

        protected BECanvasComponent _canvasReference;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            _context = await _canvasReference.CreateWebGLAsync();

            await _context.ClearColorAsync(0, 0, 0, 1); // this call does not draw anything, so it does not need to be included in the explicit batch

            await _context.BeginBatchAsync(); // begin the explicit batch

            await _context.ClearAsync(BufferBits.COLOR_BUFFER_BIT);
            await _context.DrawArraysAsync(Primitive.TRIANGLES, 0, 3);

            await _context.EndBatchAsync(); // execute all currently batched calls
        }
    }
}
