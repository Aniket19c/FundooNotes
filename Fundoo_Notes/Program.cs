using Business.Interface;
using Business.Service;
using Business.Services;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog.Web;
using Repository.Context;
using Repository.Helper;
using Repository.Interface;
using Repository.Services;
using Repository_Layer.Helper;
using StackExchange.Redis;
using System.Text;
using System.Reflection;


namespace Fundoo_Notes
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

            try
            {
                logger.Debug("Starting Fundoo Notes API setup...");

                var builder = WebApplication.CreateBuilder(args);

                //NLog 
                builder.Logging.ClearProviders();
                builder.Logging.SetMinimumLevel(LogLevel.Information);
                builder.Host.UseNLog();

               
                builder.Services.AddDbContext<UserContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("CONNECTION")));

               
                builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var configuration = builder.Configuration.GetSection("Redis")["Connection"];
                    return ConnectionMultiplexer.Connect(configuration);
                });

                
                builder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = builder.Configuration.GetConnectionString("Redis");
                    options.InstanceName = "Fundoo_Notes_";
                });


                builder.Services.AddScoped<IUserRL, UserRLImpl>();
                builder.Services.AddScoped<IUserBL, UserBLImpl>();
                builder.Services.AddScoped<INotesRL, NotesRLImpl>();
                builder.Services.AddScoped<INotesBL, NotesBLImpl>();
                builder.Services.AddScoped<ICollaboratorRL, CollaboratorRLImpl>();
                builder.Services.AddScoped<ICollaboratorBL, CollaboratorBLImpl>();
                builder.Services.AddScoped<ILabelRL, LabelRLImpl>();
                builder.Services.AddScoped<ILabelBL, LabelBLImpl>();
                builder.Services.AddScoped<JwtTokenHelper>();
                builder.Services.AddScoped<RabbitMqProducer>();
                builder.Services.AddScoped<RabbitMqConsumer>();
                builder.Services.AddScoped<RedisCacheService>();

                
                var jwtKey = builder.Configuration["Jwt:Key"];
                var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
                    };
                });

                
                builder.Services.AddControllers();

                
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo { Title = "JWT Auth API", Version = "v1" });

                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            Array.Empty<string>()
        }
    });

                    
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    options.IncludeXmlComments(xmlPath);
                });


                //CORS
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll", policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
                });

                
                var app = builder.Build();

                
                app.UseMiddleware<GlobalExceptionMiddleware>();

               
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                
                app.UseHttpsRedirection();
                app.UseCors("AllowAll");
                app.UseAuthentication();
                app.UseAuthorization();

             


                app.MapControllers();

                //To run the Consumer in background 
                using (var scope = app.Services.CreateScope())
                {
                    var consumer = scope.ServiceProvider.GetRequiredService<RabbitMqConsumer>();
                    consumer.Consume();
                }

                app.Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Application stopped due to an exception.");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }
    }
}
