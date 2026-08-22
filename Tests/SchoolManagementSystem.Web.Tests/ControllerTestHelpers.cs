using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace SchoolManagementSystem.Web.Tests;

public static class ControllerTestHelpers
{
    public static T WithTempData<T>(T controller) where T : Controller
    {
        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            new TestTempDataProvider());

        return controller;
    }
}

public sealed class TestTempDataProvider : ITempDataProvider
{
    private IDictionary<string, object> _values = new Dictionary<string, object>();

    public IDictionary<string, object> LoadTempData(HttpContext context) =>
        new Dictionary<string, object>(_values);

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
        _values = new Dictionary<string, object>(values);
    }
}
