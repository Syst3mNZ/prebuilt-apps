//  Copyright 2012  Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.IO;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using FieldService.Android.Utilities;
using FieldService.Data;
using FieldService.Utilities;
using FieldService.ViewModels;

namespace FieldService.Android.Dialogs {
    /// <summary>
    /// Dialog for the photos
    /// </summary>
    public class PhotoDialog : BaseDialog {
        PhotoViewModel photoViewModel;
        ImageView photo;
        EditText optionalCaption;
        TextView photoCount,
            dateTime;
        Bitmap imageBitmap;
        public PhotoDialog (Context context)
            : base (context)
        {
            photoViewModel = ServiceContainer.Resolve<PhotoViewModel> ();
        }

        protected override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);
            SetContentView (Resource.Layout.AddPhotoPopUpLayout);

            optionalCaption = (EditText)FindViewById (Resource.Id.photoPopupDescription);
            dateTime = (TextView)FindViewById (Resource.Id.photoDateTime);
            photoCount = (TextView)FindViewById (Resource.Id.photoCountText);

            var delete = (ImageButton)FindViewById (Resource.Id.photoDeleteImage);
            delete.Click += (sender, e) => DeletePhoto ();

            var nextPhoto = (ImageButton)FindViewById (Resource.Id.photoNextButton);
            var previousPhoto = (ImageButton)FindViewById (Resource.Id.photoPreviousButton);

            var cancel = (Button)FindViewById (Resource.Id.photoCancelImage);
            cancel.Click += (sender, e) => Dismiss ();

            var done = (Button)FindViewById (Resource.Id.photoDoneImage);
            done.Click += (sender, e) => SavePhoto ();

            photo = (ImageView)FindViewById (Resource.Id.photoImageSource);

            nextPhoto.Visibility =
                previousPhoto.Visibility =
                photoCount.Visibility = ViewStates.Invisible;
            cancel.Click += (sender, e) => Dismiss ();
            done.Click += (sender, e) => SavePhoto ();
            delete.Click += (sender, e) => DeletePhoto ();
        }

        /// <summary>
        /// Load the photo and description
        /// </summary>
        public override void OnAttachedToWindow ()
        {
            base.OnAttachedToWindow ();
            if (Photo != null) {
                dateTime.Text = string.Format ("{0} {1}", Photo.Date.ToString ("t"), Photo.Date.ToString ("d"));
                optionalCaption.Text = Photo.Description;
                if (Photo.Image != null) {
                    imageBitmap = BitmapFactory.DecodeByteArray (Photo.Image, 0, Photo.Image.Length);
                    ResizeBitmap (imageBitmap, Constants.MaxWidth, Constants.MaxHeight);
                    photo.SetImageBitmap (imageBitmap);
                }
            } else if (PhotoStream != null) {
                imageBitmap = BitmapFactory.DecodeStream (PhotoStream);
                ResizeBitmap (imageBitmap, Constants.MaxWidth, Constants.MaxHeight);
                photo.SetImageBitmap (imageBitmap);
            }
        }

        private void ResizeBitmap (Bitmap input, int destWidth, int destHeight)
        {
            int srcWidth = input.Width,
                srcHeight = input.Height;
            bool needsResize = false;
            float p;
            if (srcWidth > destWidth || srcHeight > destHeight) {
                needsResize = true;
                if (srcWidth > srcHeight && srcWidth > destWidth) {
                    p = (float)destWidth / (float)srcWidth;
                    destHeight = (int)(srcHeight * p);
                } else {
                    p = (float)destHeight / (float)srcHeight;
                    destWidth = (int)(srcWidth * p);
                }
            } else {
                destWidth = srcWidth;
                destHeight = srcHeight;
            }
            if (needsResize) {
                imageBitmap = Bitmap.CreateScaledBitmap (input, destWidth, destHeight, true);
            } 
        }

        /// <summary>
        /// Cleanup data when dialog is dismissed from window
        /// </summary>
        public override void OnDetachedFromWindow ()
        {
            base.OnDetachedFromWindow ();
            photo.SetImageBitmap (null);
            if (imageBitmap != null) {
                imageBitmap.Recycle ();
                imageBitmap.Dispose ();
                imageBitmap = null;
            }
            Activity = null;
            Assignment = null;
            Photo = null;
            PhotoStream = null;
        }

        /// <summary>
        /// The parent activity
        /// </summary>
        public Activity Activity
        {
            get;
            set;
        }

        /// <summary>
        /// The selected assignment
        /// </summary>
        public Assignment Assignment
        {
            get;
            set;
        }

        /// <summary>
        /// The photo
        /// </summary>
        public Photo Photo
        {
            get;
            set;
        }

        /// <summary>
        /// Stream passed in from MediaPicker
        /// </summary>
        public Stream PhotoStream
        {
            get;
            set;
        }

        /// <summary>
        /// Show a dialog to delete the photo
        /// </summary>
        private void DeletePhoto ()
        {
            AlertDialog.Builder deleteDialog = new AlertDialog.Builder (Context);
            deleteDialog
                .SetTitle ("Delete?")
                .SetMessage ("Are you sure?")
                .SetPositiveButton ("Yes", (sender, e) => {
                    photoViewModel
                   .DeletePhoto (Assignment, Photo)
                   .ContinueOnUIThread (_ => {
                       ((SummaryActivity)Activity).ReloadConfirmation ();
                       Dismiss ();
                   });
                })
                .SetNegativeButton ("No", (sender, e) => { })
                .Show ();
        }

        /// <summary>
        /// Save the photo
        /// </summary>
        private void SavePhoto ()
        {
            Photo savePhoto = Photo;
            if (savePhoto == null) {
                savePhoto = new Photo ();
                using (MemoryStream stream = new MemoryStream ()) {
                    if (imageBitmap.Compress (Bitmap.CompressFormat.Jpeg, 80, stream)) {
                        savePhoto.Image = stream.ToArray ();
                    }
                }
            }
            savePhoto.Description = optionalCaption.Text;
            savePhoto.Assignment = Assignment.ID;

            photoViewModel.SavePhoto (Assignment, savePhoto)
                .ContinueOnUIThread (_ => {
                    ((SummaryActivity)Activity).ReloadConfirmation ();
                    Dismiss ();
                });
        }
    }
}