using System.Linq;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Tokens;

namespace AADWebAppOIDC2 {
  public class Startup {
    public Startup(IConfiguration configuration) => Configuration = configuration;

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {
      services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApp(options => {
        Configuration.Bind("AzureAd", options);
        // Restrict users to specific belonging to specific tenants
        options.TokenValidationParameters.IssuerValidator = ValidateSpecificIssuers;
      });

      services.AddControllersWithViews(options => {
        AuthorizationPolicy policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        options.Filters.Add(new AuthorizeFilter(policy));
      });
      services.AddRazorPages().AddMicrosoftIdentityUI();
    }

    private string ValidateSpecificIssuers(string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters) {
      var validIssuers = GetAcceptedTenantIds().Select(tid => $"https://login.microsoftonline.com/{tid}/v2.0");
      if (validIssuers.Contains(issuer)) {
        return issuer;
      } else {
        throw new SecurityTokenInvalidIssuerException("The sign-in user's account does not belong to one of the tenants that this Web App accepts users from.");
      }
    }

    private string[] GetAcceptedTenantIds() {
      // If you are an ISV who wants to make the Web app available only to certain customers who are
      // paying for the service, you might want to fetch this list of accepted tenant ids from a database.
      // Here for simplicity we just return a hard-coded list of TenantIds.
      return new[] {
            "aa3efc31-e34b-4415-9c6f-aae13213cf33", // caldas.dev
            "43d106cd-a001-4919-a26f-41c820de286c" // caldas.eu
      };
    }


    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      } else {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }
      app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints => {
        endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        endpoints.MapRazorPages();
      });
    }
  }
}
