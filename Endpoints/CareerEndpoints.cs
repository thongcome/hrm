namespace HRM.Endpoints;

using HRM.Services.Rec;

// Public (unauthenticated) career-site apply submission. Routed through a
// real HTTP endpoint — not a Blazor Server event handler — for the same
// reason /login-handler is (see Endpoints/LoginEndpoints.cs): it lets
// ASP.NET Core's rate limiter actually gate it. A Blazor Server interactive
// component's button click travels over the existing SignalR circuit, not a
// fresh HTTP request, so RequireRateLimiting on an endpoint would never see
// per-submission traffic if the save happened inside the component instead.
// Components/Pages/Careers/CareerSiteApply.razor posts here via a plain
// <form method="post" enctype="multipart/form-data">, matching
// Components/Login/Login.razor's plain-form pattern.
public static class CareerEndpoints
{
    private const long MaxResumeBytes = 5 * 1024 * 1024;

    public static void MapCareerEndpoints(this WebApplication app)
    {
        app.MapPost("/careers/{postingId:long}/apply-handler", async (
            long postingId, HttpContext httpContext, RecApplicationService applicationService) =>
        {
            var form = await httpContext.Request.ReadFormAsync();
            var firstName = form["firstName"].ToString();
            var lastName = form["lastName"].ToString();
            var email = form["email"].ToString();
            var phone = form["phone"].ToString();
            var consentGiven = form["consent"].ToString() == "on";

            RecApplicationService.EducationInput? education = null;
            var eduLevel = form["edu_level"].ToString();
            if (!string.IsNullOrWhiteSpace(eduLevel))
            {
                DateOnly? eduFinished = DateOnly.TryParse(form["edu_finished"].ToString(), out var ef) ? ef : null;
                education = new RecApplicationService.EducationInput(eduLevel, form["edu_degree"].ToString(), form["edu_major"].ToString(), form["edu_institute"].ToString(), eduFinished);
            }

            var experiences = new List<RecApplicationService.ExperienceInput>();
            for (var i = 1; i <= 3; i++)
            {
                var company = form[$"exp{i}_company"].ToString();
                var position = form[$"exp{i}_position"].ToString();
                if (string.IsNullOrWhiteSpace(company) && string.IsNullOrWhiteSpace(position)) continue;
                DateOnly? start = DateOnly.TryParse(form[$"exp{i}_start"].ToString(), out var s) ? s : null;
                DateOnly? end = DateOnly.TryParse(form[$"exp{i}_end"].ToString(), out var e) ? e : null;
                experiences.Add(new RecApplicationService.ExperienceInput(position, company, start, end));
            }

            byte[]? resumeBytes = null;
            string? resumeFileName = null;
            var resumeFile = form.Files["resume"];
            if (resumeFile is not null && resumeFile.Length > 0)
            {
                if (resumeFile.Length > MaxResumeBytes)
                    return Results.LocalRedirect($"/careers/{postingId}/apply?error=" + Uri.EscapeDataString("ไฟล์เรซูเม่ต้องมีขนาดไม่เกิน 5MB"));

                using var ms = new MemoryStream();
                await resumeFile.CopyToAsync(ms);
                resumeBytes = ms.ToArray();
                resumeFileName = resumeFile.FileName;
            }

            var result = await applicationService.ApplyPublicAsync(postingId, firstName, lastName, email, phone, consentGiven, resumeFileName, resumeBytes, education, experiences);

            if (!result.Success)
                return Results.LocalRedirect($"/careers/{postingId}/apply?error=" + Uri.EscapeDataString(result.Error ?? "ไม่สามารถส่งใบสมัครได้"));

            return Results.LocalRedirect($"/careers/{postingId}/apply?submitted=true");
        }).RequireRateLimiting("career-apply");
    }
}
