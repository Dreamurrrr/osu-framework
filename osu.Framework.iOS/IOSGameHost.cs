// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using Foundation;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Video;
using osu.Framework.Input.Bindings;
using osu.Framework.IO.Stores;
using osu.Framework.iOS.Graphics.Textures;
using osu.Framework.iOS.Graphics.Video;
using osu.Framework.Platform;
using osu.Framework.Platform.MacOS;
using UIKit;

namespace osu.Framework.iOS
{
    public class IOSGameHost : SDL2GameHost
    {
        public IOSGameHost()
            : base(string.Empty)
        {
        }

        protected override IWindow CreateWindow(GraphicsSurfaceType preferredSurface) => new IOSWindow(preferredSurface);

        protected override void SetupForRun()
        {
            base.SetupForRun();

            AllowScreenSuspension.Result.BindValueChanged(allow =>
                    InputThread.Scheduler.Add(() => UIApplication.SharedApplication.IdleTimerDisabled = !allow.NewValue),
                true);
        }

        protected override void SetupConfig(IDictionary<FrameworkSetting, object> defaultOverrides)
        {
            if (!defaultOverrides.ContainsKey(FrameworkSetting.ExecutionMode))
                defaultOverrides.Add(FrameworkSetting.ExecutionMode, ExecutionMode.SingleThread);

            base.SetupConfig(defaultOverrides);

            DebugConfig.SetValue(DebugSetting.BypassFrontToBackPass, true);
        }

        public override bool OnScreenKeyboardOverlapsGameWindow => true;

        public override bool CanExit => false;

        public override Storage GetStorage(string path) => new IOSStorage(path, this);

        public override bool OpenFileExternally(string filename) => false;

        public override bool PresentFileExternally(string filename) => false;

        public override void OpenUrlExternally(string url)
        {
            if (!url.CheckIsValidUrl())
                throw new ArgumentException("The provided URL must be one of either http://, https:// or mailto: protocols.", nameof(url));

            UIApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                NSUrl nsurl = NSUrl.FromString(url).AsNonNull();
                if (UIApplication.SharedApplication.CanOpenUrl(nsurl))
                    UIApplication.SharedApplication.OpenUrl(nsurl, new NSDictionary(), null);
            });
        }

        public override IResourceStore<TextureUpload> CreateTextureLoaderStore(IResourceStore<byte[]> underlyingStore)
            => new IOSTextureLoaderStore(underlyingStore);

        public override VideoDecoder CreateVideoDecoder(Stream stream)
            => new IOSVideoDecoder(Renderer, stream);

        public override IEnumerable<KeyBinding> PlatformKeyBindings => MacOSGameHost.KeyBindings;
    }
}
