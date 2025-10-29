using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CCEAPI.Data;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace CCEAPI.Services
{
    public interface IImageService
    {
        Task GenerateSummaryImageAsync();
        byte[]? GetSummaryImage();
    }

    public class ImageService : IImageService
    {
        private readonly AppDbContext _context;
        private readonly string _imagePath = Path.Combine("cache", "summary.png");

        public ImageService(AppDbContext context)
        {
            _context = context;
            
            // Create cache directory if it doesn't exist
            var directory = Path.GetDirectoryName(_imagePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Console.WriteLine($"Creating cache directory: {directory}");
                Directory.CreateDirectory(directory);
            }
        }

        public async Task GenerateSummaryImageAsync()
        {
            // Get total countries count
            var totalCountries = await _context.Countries.CountAsync();
            
            // Get top 5 countries by estimated GDP
            var topCountries = await _context.Countries
                .Where(c => c.EstimatedGdp != null && c.EstimatedGdp > 0)
                .OrderByDescending(c => c.EstimatedGdp)
                .Take(5)
                .Select(c => new { c.Name, c.EstimatedGdp })
                .ToListAsync();

            // Get last refresh timestamp
            var metadata = await _context.RefreshMetadata.FirstOrDefaultAsync();
            var lastRefresh = metadata?.LastRefreshedAt ?? DateTime.UtcNow;

            // Image dimensions
            const int width = 800;
            const int height = 600;

            // Create image surface
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;

            // Draw white background
            canvas.Clear(SKColors.White);

            // Draw border
            using var borderPaint = new SKPaint
            {
                Color = SKColors.DarkBlue,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3,
                IsAntialias = true
            };
            canvas.DrawRect(10, 10, width - 20, height - 20, borderPaint);

            // Title
            using var titlePaint = new SKPaint
            {
                Color = SKColors.DarkBlue,
                TextSize = 40,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };
            canvas.DrawText("Country Currency Summary", 50, 70, titlePaint);

            // Draw separator line
            using var linePaint = new SKPaint
            {
                Color = SKColors.LightGray,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            canvas.DrawLine(50, 90, width - 50, 90, linePaint);

            // Total countries text
            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 26,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };
            canvas.DrawText($"Total Countries: {totalCountries}", 50, 140, textPaint);

            // Top 5 header
            using var headerPaint = new SKPaint
            {
                Color = SKColors.DarkBlue,
                TextSize = 28,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };
            canvas.DrawText("Top 5 Countries by Estimated GDP:", 50, 200, headerPaint);

            // List top countries
            using var countryPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 22,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial")
            };

            var yPosition = 245;
            var rank = 1;
            
            foreach (var country in topCountries)
            {
                var gdpFormatted = country.EstimatedGdp?.ToString("N2") ?? "N/A";
                var text = $"{rank}. {country.Name}: ${gdpFormatted}";
                canvas.DrawText(text, 70, yPosition, countryPaint);
                yPosition += 45;
                rank++;
            }

            // If less than 5 countries, show message
            if (topCountries.Count < 5)
            {
                using var notePaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    TextSize = 18,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Italic)
                };
                canvas.DrawText($"(Only {topCountries.Count} countries with valid GDP data)", 70, yPosition, notePaint);
            }

            // Draw bottom separator line
            canvas.DrawLine(50, height - 100, width - 50, height - 100, linePaint);

            // Timestamp
            using var timestampPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextSize = 20,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Italic)
            };
            var timestampText = $"Last Refreshed: {lastRefresh:yyyy-MM-dd HH:mm:ss} UTC";
            canvas.DrawText(timestampText, 50, height - 50, timestampPaint);

            // Save image to disk
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(_imagePath);
            data.SaveTo(stream);
        }

        public byte[]? GetSummaryImage()
        {
            if (!File.Exists(_imagePath))
            {
                return null;
            }

            return File.ReadAllBytes(_imagePath);
        }
    }
}