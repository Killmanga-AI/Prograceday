using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public sealed class EventsController(RaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Browse(string? search, string? type)
    {
        var query = "api/events";
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) parameters.Add("search="+Uri.EscapeDataString(search));
        if (!string.IsNullOrWhiteSpace(type)) parameters.Add("type="+Uri.EscapeDataString(type));
        parameters.Add("from="+Uri.EscapeDataString(DateTime.Today.ToString("O")));
        if(parameters.Count>0) query += "?"+string.Join("&",parameters);
        var result = await api.GetAsync<List<EventDto>>(query);
        ViewBag.Error=result.Error; ViewBag.Search=search; ViewBag.Type=type;
        return View(result.Data ?? []);
    }
    public async Task<IActionResult> Details(int id)
    {
        var ev=await api.GetAsync<EventDto>($"api/events/{id}");
        if(!ev.Success || ev.Data is null) return NotFound();
        var cats=await api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
        ViewBag.Categories=cats.Data ?? [];
        ViewBag.Error=cats.Error;
        return View(ev.Data);
    }
    [SessionAuthorize("Participant"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        var result=await api.PostAsync<System.Text.Json.JsonElement>($"api/categories/{categoryId}/enrolments",new {});
        TempData[result.Success?"Success":"Error"]=result.Success?"You are enrolled successfully.":result.Error;
        return RedirectToAction(nameof(Details),new{id=eventId});
    }
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> MyEvents()
    {
        var result=await api.GetAsync<List<System.Text.Json.JsonElement>>("api/users/me/enrolments");
        ViewBag.Error=result.Error; return View(result.Data ?? []);
    }
    [SessionAuthorize("Participant"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result=await api.DeleteAsync<bool>($"api/enrolments/{id}");
        TempData[result.Success?"Success":"Error"]=result.Success?"Enrolment cancelled.":result.Error;
        return RedirectToAction(nameof(MyEvents));
    }
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> MyResults()
    {
        var result=await api.GetAsync<List<System.Text.Json.JsonElement>>("api/users/me/results");
        ViewBag.Error=result.Error; return View(result.Data ?? []);
    }
}
