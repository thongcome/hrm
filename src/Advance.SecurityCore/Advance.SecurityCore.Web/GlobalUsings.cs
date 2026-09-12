// This project uses Microsoft.NET.Sdk.Razor, which — unlike
// Microsoft.NET.Sdk.Web — does NOT automatically add ASP.NET Core's usual
// set of implicit global usings (Microsoft.AspNetCore.Http,
// Microsoft.AspNetCore.Builder, Microsoft.Extensions.DependencyInjection,
// etc.). HRM's original files (Program.cs, Endpoints/LoginEndpoints.cs,
// Components/Account/**) all compiled under Sdk.Web and so never needed
// these usings written out explicitly. Rather than hand-patch every one of
// the ~40 copied Razor/C# files with the exact subset it happens to need,
// this single file declares them globally for the whole project — the same
// net effect as Sdk.Web's own implicit list, scoped to this project only.
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
