using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace BlogReview.Services
{
    public class ImageStorage
    {
        private readonly Cloudinary cloudinary;
        public ImageStorage(Account account) 
        { 
            cloudinary = new Cloudinary(account);
        }
        public async Task<ImageUploadResult> UploadImage(IFormFile image)
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(image.FileName, image.OpenReadStream()),
                //PublicId = "your_image_public_id",
                Folder = "folder"
            };
            return await cloudinary.UploadAsync(uploadParams);
        }
        public async Task<DeletionResult> DeleteImage(string publicId)
        {
            DeletionParams deletionParams = new DeletionParams(publicId);
            return await cloudinary.DestroyAsync(deletionParams);
        }
        public string GetUrlById(string publicId)
        {
            return cloudinary.GetResource(publicId).SecureUrl;
        }
    }
}
