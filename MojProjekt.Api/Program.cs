var builder = WebApplication.CreateBuilder(args);

// [] Albo proxy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// [] Wlaczenie policy w ciągu middleware
app.UseCors("AllowAngular");

app.UseHttpsRedirection();

app.Run();