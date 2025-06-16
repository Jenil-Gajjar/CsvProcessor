using CsvProcessor.BAL.Interface;
using CsvProcessor.BAL.Implementation;
using CsvProcessor.DAL.Interface;
using CsvProcessor.DAL.Implementation;
using CsvProcessor.BAL.Helper;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IVariantRepository, VariantRepository>();
builder.Services.AddScoped<IShippingRepository, ShippingRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();

builder.Services.AddHostedService<FileDonwloaderWorker>();
builder.Services.AddHttpClient<IImageService, ImageService>();
builder.Services.AddScoped<ICsvProcessorService, CsvProcessorService>();
builder.Services.AddScoped<IImageService, ImageService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=CsvProcessor}/{action=Index}");

app.Run();
