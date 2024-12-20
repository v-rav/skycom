//=============================================================================
// Startup
// プログラム起動時に１回だけ実行する（初期設定）用ファイル
//
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using Blazored.LocalStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SKYCOM.DLManagement.Data;
using SKYCOM.DLManagement.Entity;
using SKYCOM.DLManagement.Services;
using System;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace SKYCOM.DLManagement
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
            services.AddBlazoredLocalStorage();
            services.AddSingleton<Message>(new Message(Configuration.GetSection("MessagePath").Value,Configuration.GetSection("StorageContainerName").Value));
            services.AddOptions<Settings>().Bind(Configuration.GetSection("Settings"));
            services.AddMvc(options => options.EnableEndpointRouting = false);
            //services.AddDbContext<DbAccess>(options => options.UseMySql(Configuration.GetConnectionString("DefaultConnection")));
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<DbAccess>(options => options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 3))));
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddScoped<FileManagementService>();
            services.AddScoped<ReleaseGuideService>();
            services.AddScoped<DLStatusManagementService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
#pragma warning disable CA1822 // メンバーを static に設定します
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
#pragma warning restore CA1822 // メンバーを static に設定します
        {
            loggerFactory.AddLog4Net();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseMvcWithDefaultRoute();

            app.UseSession();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
