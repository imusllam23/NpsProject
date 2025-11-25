namespace NpsProject.Helpers
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(IFormFile imageFile, string folderName);
        void DeleteImage(string imageUrl);
        bool IsValidImage(IFormFile imageFile);
    }

}
