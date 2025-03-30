using System;
using System.Collections.Generic;
using System.Linq;
using Citron.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebPush;

namespace Citron.Backend
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<MyDbContext>(o => {
                o.UseSqlite("Data Source=app.db");
                
                o.LogTo(message => System.Diagnostics.Debug.WriteLine(message));
            });
            services.AddSignalR();
            services.AddCors();
            services.AddMvc();
            services.AddControllers();
            services.AddSingleton<SingleInstanceHelper>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors(builder => builder
                .WithOrigins("https://10.10.10.106:5173", "https://localhost:5173", "https://10.10.10.118:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
            );
            //app.UseMvc();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                     name: "default",
                        pattern: "{controller=Api}/{action=Index}/");
                endpoints.MapHub<ApplicationHub>("/index");
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); });
            });
        }
    }

    internal class SingleInstanceHelper
    {
        //public WebPushClient PushClient { get; set; }
        public List<string> Connections { get; set; } = new List<string>();
    }
}