using System;
using System.IO;
using System.Reflection;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LookupServices;
using JCCommon.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scv.Api.Helpers.Mapping;
using Scv.Api.Helpers.Middleware;
using Scv.Api.Services;

namespace Scv.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMapster();

            string corsDomain = Configuration.GetValue<string>("CORS_DOMAIN");
            Console.WriteLine($"CORS_DOMAIN: {corsDomain}");

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins(corsDomain);
                });
            });

            services.AddHttpClient<FileServicesClient>(client =>
            {
                client.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(
                    Configuration.GetValue<string>("FileServicesClient:Username"),
                    Configuration.GetValue<string>("FileServicesClient:Password"));
            });

            services.AddHttpClient<LookupServiceClient>(client =>
            {
                client.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(
                    Configuration.GetValue<string>("LookupServicesClient:Username"),
                    Configuration.GetValue<string>("LookupServicesClient:Password"));
            });

            services.AddScoped<FilesService>();
            services.AddScoped<LookupService>();

            services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

            services.AddControllers().AddNewtonsoftJson();

            services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations(true);
                options.CustomSchemaIds(o => o.FullName);
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });
            services.AddLazyCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

 	        app.UseCors();

            app.UseSwagger(options => 
            {
                options.RouteTemplate = "api/swagger/{documentname}/swagger.json";
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("swagger/v1/swagger.json", "SCV.API");
                options.RoutePrefix = "api";
            });

            app.UseMiddleware(typeof(ErrorHandlingMiddleware));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
