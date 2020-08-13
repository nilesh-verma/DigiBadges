using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.UI.Services;
using DigiBadges.Utility;
using Microsoft.AspNetCore.Http;
using DigiBadges.Models;
using DigiBadges.DataAccess.Repository;
using Microsoft.Extensions.Options;
using DigiBadges.DataAccess.Data;
using SolrNet;

namespace DigiBadges
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            ENV = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment ENV { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                })
               .AddCookie(configureOptions =>
               {
                   configureOptions.LoginPath = "/Auth/Login";
                   configureOptions.ForwardChallenge = CookieAuthenticationDefaults.AuthenticationScheme;
                   configureOptions.ForwardSignIn = CookieAuthenticationDefaults.AuthenticationScheme;
                   configureOptions.ForwardAuthenticate = CookieAuthenticationDefaults.AuthenticationScheme;
                   configureOptions.AccessDeniedPath = "/Identity/Account/AccessDenied";
               })
               .AddFacebook(options =>
               {
                   options.AppId = Configuration.GetSection("FacebookSettings").GetValue<string>("AppId");
                   options.AppSecret = Configuration.GetSection("FacebookSettings").GetValue<string>("AppSecret");
                   options.AccessDeniedPath = "/Identity/Account/AccessDenied";
               })
               .AddTwitter(options =>
               {
                   options.ConsumerKey = Configuration.GetSection("TwitterSettings").GetValue<string>("ConsumerKey");
                   options.ConsumerSecret = Configuration.GetSection("TwitterSettings").GetValue<string>("ConsumerSecret");
                   options.RetrieveUserDetails = true;
                   options.AccessDeniedPath = "/Identity/Account/AccessDenied";
               })

               .AddGoogle(options =>
               {
                   options.ClientId = Configuration.GetSection("GooogleSettings").GetValue<string>("ClientId");
                   options.ClientSecret = Configuration.GetSection("GooogleSettings").GetValue<string>("ClientSecret");
                   options.AccessDeniedPath = "/Identity/Account/AccessDenied";
               })

             .AddLinkedIn(options =>
              {
                  options.ClientId = Configuration.GetSection("LinkedInSettings").GetValue<string>("ClientId");
                  options.ClientSecret = Configuration.GetSection("LinkedInSettings").GetValue<string>("ClientSecret");
                  options.AccessDeniedPath = "/Identity/Account/AccessDenied";
              });

            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddSingleton<AppUser, AppUser>();
            services.Configure<EmailOptions>(Configuration.GetSection("EmailService"));
            services.AddSingleton<IConfiguration>(Configuration);


            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped(typeof(Repository<>));
            services.Configure<MongoDbSetting>(Configuration.GetSection("MongoDb"));
            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            if (ENV.IsDevelopment() || ENV.IsStaging())
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    //options.Configuration = "localhost";
                    //options.InstanceName = "DIGI/"; // For App Isolation - Prefix automatically added to any key written to the cache
                    options.Configuration = Configuration.GetSection("RedisEndPoints").GetValue<string>("Configuration");
                    options.InstanceName = Configuration.GetSection("RedisEndPoints").GetValue<string>("InstanceName");
                });

                string solrCoreName = Configuration.GetSection("SolrEndPoints").GetValue<string>("UserCoreName");
                services.AddSolrNet<SolrUsersModel>(solrCoreName);
                services.AddSolrNet<SolrIssuersModel>(Configuration.GetSection("SolrEndPoints").GetValue<string>("IssuerCoreName").ToString());
                services.AddSolrNet<SolrBadgeModel>(Configuration.GetSection("SolrEndPoints").GetValue<string>("BadgeCoreName").ToString());

            }
            if (ENV.IsProduction())
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    //options.Configuration = "localhost";
                    //options.InstanceName = "DIGI/"; // For App Isolation - Prefix automatically added to any key written to the cache
                    options.Configuration = Configuration.GetSection("DigiInProd").GetSection("RedisEndPoints").GetValue<string>("Configuration");
                    options.InstanceName = Configuration.GetSection("DigiInProd").GetSection("RedisEndPoints").GetValue<string>("InstanceName");
                });

                string solrCoreName = Configuration.GetSection("DigiInProd").GetSection("SolrEndPoints").GetValue<string>("UserCoreName");
                services.AddSolrNet<SolrUsersModel>(solrCoreName);
                services.AddSolrNet<SolrIssuersModel>(Configuration.GetSection("DigiInProd").GetSection("SolrEndPoints").GetValue<string>("IssuerCoreName").ToString());
                services.AddSolrNet<SolrBadgeModel>(Configuration.GetSection("DigiInProd").GetSection("SolrEndPoints").GetValue<string>("BadgeCoreName").ToString());

            }
            //services.AddSolrNet<SolrUsersModel>($"http://localhost:8983/solr/NewDigiData_Test04072020");
            //services.AddSolrNet<SolrIssuersModel>($"http://localhost:8983/solr/NewDigiData_Issuer10072020");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseStatusCodePages();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            appLifetime.ApplicationStarted.Register(OnStarted);

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{area=Employee}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
        private void OnStarted()
        {
            var dbInit = new DBInit(Configuration);
            dbInit.InitializeUserRoles();
            dbInit.CreateSuperAdmin();
        }
    }
}
