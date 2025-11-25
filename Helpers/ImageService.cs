namespace NpsProject.Helpers
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ImageService> _logger;
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public ImageService(IWebHostEnvironment webHostEnvironment, ILogger<ImageService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        /// <summary>
        /// حفظ الصورة في المجلد المحدد
        /// </summary>
        /// <param name="imageFile">ملف الصورة</param>
        /// <param name="folderName">اسم المجلد (مثل: "news", "projects")</param>
        /// <returns>المسار النسبي للصورة</returns>
        public async Task<string> SaveImageAsync(IFormFile imageFile, string folderName)
        {
            if (imageFile == null || imageFile.Length == 0)
                throw new ArgumentException("الملف غير صالح");

            // التحقق من صحة الصورة
            if (!IsValidImage(imageFile))
                throw new InvalidOperationException("نوع الملف غير مدعوم");

            // التحقق من حجم الملف
            if (imageFile.Length > _maxFileSize)
                throw new InvalidOperationException($"حجم الملف يتجاوز الحد الأقصى ({_maxFileSize / 1024 / 1024}MB)");

            try
            {
                // إنشاء المجلد إذا لم يكن موجوداً
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", folderName);
                Directory.CreateDirectory(uploadsFolder);

                // إنشاء اسم فريد للملف
                string fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // حفظ الملف
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                _logger.LogInformation($"تم حفظ الصورة: {uniqueFileName} في {folderName}");

                // إرجاع المسار النسبي
                return $"/images/{folderName}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل حفظ الصورة");
                throw new InvalidOperationException("فشل في حفظ الصورة", ex);
            }
        }

        /// <summary>
        /// حذف الصورة من الخادم
        /// </summary>
        /// <param name="imageUrl">المسار النسبي للصورة</param>
        public void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            try
            {
                string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));

                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    _logger.LogInformation($"تم حذف الصورة: {imageUrl}");
                }
                else
                {
                    _logger.LogWarning($"الصورة غير موجودة: {imageUrl}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"فشل حذف الصورة: {imageUrl}");
                // لا نرمي استثناء هنا لأن حذف الصورة ليس حرجاً
            }
        }

        /// <summary>
        /// التحقق من صحة الصورة
        /// </summary>
        public bool IsValidImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return false;

            // التحقق من الامتداد
            string extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return false;

            // التحقق من نوع المحتوى
            if (!imageFile.ContentType.StartsWith("image/"))
                return false;

            return true;
        }
    }
}
