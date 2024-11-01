using Firebase.Auth;
using Firebase.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FBoothApp.Helpers
{
    public class FirebaseHelper
    {
        private string _apiKey;
        private string _bucket;
        private string _authEmail;
        private string _authPassword;

        // Constructor để khởi tạo thông tin Firebase trực tiếp
        public FirebaseHelper(string apiKey, string bucket, string authEmail, string authPassword)
        {
            _apiKey = apiKey;
            _bucket = bucket;
            _authEmail = authEmail;
            _authPassword = authPassword;
        }

        // Xác thực với Firebase bằng email và mật khẩu
        private async Task<FirebaseAuthLink> AuthenticateFirebaseAsync()
        {
            var authProvider = new FirebaseAuthProvider(new FirebaseConfig(_apiKey));
            var auth = await authProvider.SignInWithEmailAndPasswordAsync(_authEmail, _authPassword);
            return auth;
        }

        // Tải ảnh lên Firebase Storage và trả về URL ảnh
        public async Task<string> UploadImageToFirebaseAsync(string localImagePath, string folder)
        {
            try
            {
                var auth = await AuthenticateFirebaseAsync(); // Xác thực trước khi tải lên
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // Tạo tên file duy nhất dựa trên timestamp
                string objectName = $"{folder}/{Path.GetFileNameWithoutExtension(localImagePath)}_{timestamp}.jpg";

                // Mở stream từ file ảnh
                using (var stream = new FileStream(localImagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var firebaseStorage = new FirebaseStorage(
                        _bucket,
                        new FirebaseStorageOptions
                        {
                            AuthTokenAsyncFactory = () => Task.FromResult(auth.FirebaseToken),
                            ThrowOnCancel = true
                        });

                    // Tải ảnh lên Firebase Storage
                    var task = await firebaseStorage
                        .Child(objectName)
                        .PutAsync(stream);

                    string imageUrl = task; // URL của ảnh đã tải lên
                    return imageUrl;
                }
            }

            catch (Exception ex)
            {
                throw new Exception($"Failed to upload image: {ex.Message}", ex);
            }
        }
    }
}
