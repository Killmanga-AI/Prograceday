using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize("Organiser")]
public sealed class OrganiserController(RaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        var r=await api.GetAsync<System.Text.Json.JsonElement>("api/organiser/dashboard");
        ViewBag.Error=r.Error; return View(r.Data);
    }
    public async Task<IActionResult> Events()
    {
        var r=await api.GetAsync<List<System.Text.Json.JsonElement>>("api/organiser/events");
        ViewBag.Error=r.Error; return View(r.Data ?? []);
    }
    [HttpGet] public IActionResult CreateEvent() => View(new EventViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventViewModel model)
    {
        if(!ModelState.IsValid) return View(model);
        var r=await api.PostAsync<EventDto>("api/events",new {model.EventName,model.Description,model.EventDate,model.Location,model.RouteInfo,model.EventDistanceKm,model.EventType});
        if(!r.Success || r.Data is null) { ModelState.AddModelError("",r.Error??"Could not create event."); return View(model); }
        if(model.Banner is not null)
        {
            var upload=await api.UploadAsync<System.Text.Json.JsonElement>($"api/events/{r.Data.EventId}/banner",model.Banner);
            if(!upload.Success) TempData["Error"]="Event created, but banner upload failed: "+upload.Error;
        }
        TempData["Success"]="Event created.";
        return RedirectToAction(nameof(EventCategories),new{id=r.Data.EventId});
    }
    [HttpGet] public async Task<IActionResult> EditEvent(int id)
    {
        if(!await OwnsEventAsync(id)) return RedirectToAction(nameof(Events));
        var r=await api.GetAsync<EventDto>($"api/events/{id}");
        if(!r.Success || r.Data is null) return NotFound();
        var e=r.Data;
        return View(new EventViewModel{EventId=e.EventId,EventName=e.EventName,Description=e.Description,EventDate=e.EventDate,Location=e.Location,RouteInfo=e.RouteInfo,EventDistanceKm=e.EventDistanceKm,EventType=e.EventType,BannerImageUrl=e.BannerImageUrl});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventViewModel model)
    {
        if(!ModelState.IsValid) return View(model);
        var r=await api.PutAsync<EventDto>($"api/events/{model.EventId}",new {model.EventName,model.Description,model.EventDate,model.Location,model.RouteInfo,model.EventDistanceKm,model.EventType});
        if(!r.Success) { ModelState.AddModelError("",r.Error??"Update failed."); return View(model); }
        if(model.Banner is not null)
        {
            var upload=await api.UploadAsync<System.Text.Json.JsonElement>($"api/events/{model.EventId}/banner",model.Banner);
            if(!upload.Success) TempData["Error"]="Event updated, but banner upload failed: "+upload.Error;
        }
        TempData["Success"]="Event updated."; return RedirectToAction(nameof(Events));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var r=await api.DeleteAsync<bool>($"api/events/{id}");
        TempData[r.Success?"Success":"Error"]=r.Success?"Event deleted.":r.Error;
        return RedirectToAction(nameof(Events));
    }
    public async Task<IActionResult> EventCategories(int id)
    {
        if(!await OwnsEventAsync(id)) return RedirectToAction(nameof(Events));
        var ev=await api.GetAsync<EventDto>($"api/events/{id}");
        if(!ev.Success || ev.Data is null) return NotFound();
        var cats=await api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
        ViewBag.Event=ev.Data; ViewBag.Error=cats.Error; return View(cats.Data??[]);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryViewModel model)
    {
        if(!ModelState.IsValid) { TempData["Error"]="Please provide a valid category."; return RedirectToAction(nameof(EventCategories),new{id=model.EventId}); }
        var r=await api.PostAsync<System.Text.Json.JsonElement>($"api/events/{model.EventId}/categories",new{model.CategoryName,model.DistanceKm,model.MaxParticipants});
        TempData[r.Success?"Success":"Error"]=r.Success?"Category added.":r.Error;
        return RedirectToAction(nameof(EventCategories),new{id=model.EventId});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(CategoryViewModel model)
    {
        var r=await api.PutAsync<System.Text.Json.JsonElement>($"api/categories/{model.CategoryId}",new{model.CategoryName,model.DistanceKm,model.MaxParticipants});
        TempData[r.Success?"Success":"Error"]=r.Success?"Category updated.":r.Error;
        return RedirectToAction(nameof(EventCategories),new{id=model.EventId});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id,int eventId)
    {
        var r=await api.DeleteAsync<bool>($"api/categories/{id}");
        TempData[r.Success?"Success":"Error"]=r.Success?"Category deleted.":r.Error;
        return RedirectToAction(nameof(EventCategories),new{id=eventId});
    }
    public async Task<IActionResult> Enrolments(int id)
    {
        var ev=await api.GetAsync<EventDto>($"api/events/{id}");
        if(!ev.Success || ev.Data is null) return NotFound();
        var r=await api.GetAsync<List<System.Text.Json.JsonElement>>($"api/events/{id}/enrolments");
        ViewBag.Event=ev.Data; ViewBag.Error=r.Error; return View(r.Data??[]);
    }
    public async Task<IActionResult> Results(int id)
    {
        var ev=await api.GetAsync<EventDto>($"api/events/{id}");
        if(!ev.Success || ev.Data is null) return NotFound();
        var enrol=await api.GetAsync<List<System.Text.Json.JsonElement>>($"api/events/{id}/enrolments");
        if(!enrol.Success) { TempData["Error"]=enrol.Error??"You cannot manage this event."; return RedirectToAction(nameof(Events)); }
        var results=await api.GetAsync<List<System.Text.Json.JsonElement>>($"api/events/{id}/results");
        ViewBag.Event=ev.Data; ViewBag.Enrolments=enrol.Data??[]; ViewBag.Error=results.Error; return View(results.Data??[]);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddResult(ResultViewModel model,int eventId)
    {
        var r=await api.PostAsync<System.Text.Json.JsonElement>($"api/enrolments/{model.EnrolmentId}/results",new{model.FinishTime,model.Position});
        TempData[r.Success?"Success":"Error"]=r.Success?"Result recorded.":r.Error;
        return RedirectToAction(nameof(Results),new{id=eventId});
    }

    private async Task<bool> OwnsEventAsync(int id)
    {
        var mine=await api.GetAsync<List<System.Text.Json.JsonElement>>("api/organiser/events");
        return mine.Success && mine.Data is not null && mine.Data.Any(item =>
            item.GetProperty("eventId").GetInt32() == id);
    }
}
