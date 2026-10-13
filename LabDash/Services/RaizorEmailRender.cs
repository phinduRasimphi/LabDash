using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace LabDash.Services
{
    public interface IRazorEmailRenderer
    {
        Task<string> RenderAsync<TModel>(
            string viewPath,
            TModel model,
            HttpContext httpContext);
    }

    public class RazorEmailRenderer : IRazorEmailRenderer
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;

        public RazorEmailRenderer(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
        }

        public async Task<string> RenderAsync<TModel>(
            string viewPath,
            TModel model,
            HttpContext httpContext)
        {
            var actionContext = new ActionContext(
                httpContext,
                httpContext.GetRouteData(),
                new ActionDescriptor());

            var view = FindView(actionContext, viewPath);

            await using var writer = new StringWriter();

            var viewData = new ViewDataDictionary<TModel>(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
            {
                Model = model
            };

            var tempData = new TempDataDictionary(httpContext, _tempDataProvider);

            var viewContext = new ViewContext(
                actionContext,
                view,
                viewData,
                tempData,
                writer,
                new HtmlHelperOptions());

            await view.RenderAsync(viewContext);

            return writer.ToString();
        }

        private IView FindView(ActionContext context, string viewPath)
        {
            var getViewResult = _viewEngine.GetView(
                executingFilePath: null,
                viewPath: viewPath,
                isMainPage: true);

            if (getViewResult.Success)
            {
                return getViewResult.View;
            }

            var findViewResult = _viewEngine.FindView(
                context, viewPath, isMainPage: true);

            if (findViewResult.Success)
            {
                return findViewResult.View;
            }

            throw new InvalidOperationException(
                $"Unable to find view '{viewPath}'.");
        }
    }
}