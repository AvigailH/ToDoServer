using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);
 
var connectionString = builder.Configuration.GetConnectionString("ToDoDB");

builder.Services.AddDbContextPool<ToDoDbContext>(options =>
{
    options.UseMySql(
        connectionString, 
        ServerVersion.AutoDetect(connectionString),
        options => options.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: System.TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    );
});


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("MyPolicy", policy =>
//     {
//         policy.AllowAnyOrigin()  
//               .AllowAnyMethod()
//               .AllowAnyHeader();
//     });
// });


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CodeTimeSwaggerDemo", Version = "v1" });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeTimeSwaggerDemo v1");
        c.RoutePrefix = "";
    });
}

app.UseAuthorization();
app.UseRouting();
app.UseCors();

app.MapControllers();

app.MapGet("/items", async (ToDoDbContext dbContext) =>
{
    try
    {
        var items = await dbContext.Items.ToListAsync();
        return Results.Ok(items);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
}).WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

app.MapPost("/", async (ToDoDbContext dbContext, [FromBody] Item newItem) =>
{
    try
    {
        dbContext.Items.Add(newItem);
        await dbContext.SaveChangesAsync();
        return Results.Created($"/{newItem.Id}", newItem);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
}).WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

app.MapPut("/{id}", async (ToDoDbContext dbContext, int id, [FromBody] Item updatedItem) =>
{
    try
    {
        var existingItem = await dbContext.Items.FindAsync(id);
        if (existingItem == null)
        {
            return Results.NotFound();
        }

        existingItem.Name = updatedItem.Name;
        existingItem.IsComplete = updatedItem.IsComplete;
        await dbContext.SaveChangesAsync();
        return Results.Ok(existingItem);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
}).WithMetadata(new HttpMethodMetadata(new[] { "PUT" }));

app.MapDelete("/{id}", async (ToDoDbContext dbContext, int id) =>
{
    try
    {
        var existingItem = await dbContext.Items.FindAsync(id);
        if (existingItem == null)
        {
            return Results.NotFound();
        }
        dbContext.Items.Remove(existingItem);
        await dbContext.SaveChangesAsync();
        return Results.Ok(existingItem);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
}).WithMetadata(new HttpMethodMetadata(new[] { "DELETE" }));

app.MapGet("/", () => "API is running!!!");

app.Run();
