using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AppService.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace ServiceB
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
           //AddDapr() registers the Dapr integration with controllers.
           //This also registers the DaprClient service with the dependency injection
           //container (using the sepcified DaprClientBuilder for settings options).
           //This service can be used to interact with the dapr runtime
           //(e.g. invoke services, publish messages, interact with a state-store, ...).
            services.AddControllers().AddDapr(builder => 
                builder.UseJsonSerializationOptions(
                    new JsonSerializerOptions()
                    {
                           PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true,
                    }));
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ServiceB", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ServiceB v1"));
            }
            // app.UseHttpsRedirection();
            app.UseRouting();
            //UseCloudEvents() registers the Cloud Events middleware in
            //the request processing pipeline.
            //This middleware will unwrap requests with
            //Content-Type application/cloudevents+json so that model binding
            //can access the event payload in the request body directly.
            //This is recommended when using pub/sub unless
            //you have a need to process the event metadata yourself.
            app.UseCloudEvents();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => {
                //MapSubscribeHandler() registers an endpoint that will be called by
                //the Dapr runtime to register for pub/sub topics.
                //This is is not needed unless using pub/sub.
                endpoints.MapSubscribeHandler(); //
                endpoints.MapControllers();
                // endpoints.MapGrpcService<BankingGrpcService>();
            });
        }
    }
}
