using WebApplication1.Interfaces.IDssService;
using WebApplication1.Interfaces.IMD5Service;
using WebApplication1.Interfaces.IPseudoGeneratorService;
using WebApplication1.Interfaces.IRc5Service;
using WebApplication1.Interfaces.IRsaService;
using WebApplication1.Services.DssService;
using WebApplication1.Services.MD5Service;
using WebApplication1.Services.PseudoGenService;
using WebApplication1.Services.RC5Service;
using WebApplication1.Services.RsaService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IPseudoGeneratorService, PseudoGenService>(provider =>
    new PseudoGenService(new Random(), (ulong)DateTime.Now.Ticks));
builder.Services.AddScoped<IMd5Service, Md5Service>();
builder.Services.AddScoped<IRc5Service, Rc5Service>();
builder.Services.AddScoped<IRsaService, RsaService>();
builder.Services.AddSingleton<IDssService, DssService>();
builder.Services.AddSingleton<Random>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

app.Run();
