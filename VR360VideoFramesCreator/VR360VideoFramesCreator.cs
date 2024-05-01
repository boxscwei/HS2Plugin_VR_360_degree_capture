using BepInEx;
using BepInEx.Configuration;
using BepisPlugins;
using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using Pngcs.Unity;
/**
* 感谢github上的几个人，他们的源代码提供了基本思路。我经过千百次的测试和资料的查阅，写成了这个插件。
* 特别鸣谢：gua_jun，BepisPlugins, IllusionMods，Bepis，Unity手册等。
* 现在这个版本还有点bug，不能支持部分特效。
* by boxsc 
*/
namespace VR360VideoFramesCreator
{

    [BepInPlugin("com.boxscwei.VR360VideoFramesCreator", "VR360VideoFramesCreator", "3.0")]
    [BepInProcess("StudioNEOV2.exe")]

    public class VR360VideoFramesCreator : BaseUnityPlugin
    {
        /// <summary>
        /// GUID of the plugin, use with BepInDependency
        /// </summary>
        public const string GUID = "com.boxscwei.VR360VideoFramesCreator";
        /// <summary>
        /// Version of the plugin, use with BepInDependency
        /// </summary>
        public const string Version = "3.0";
        /// <summary>
        /// Triggered before a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPreCapture;
        /// <summary>
        /// Triggered after a screenshot is captured. For use by plugins adding screen effects incompatible with Screencap.
        /// </summary>
        public static event Action OnPostCapture;

        enum FOVangle
        {
            All_360_Degree = 63,
            NoBackward_270_Degree = 31
        };
        enum Picture_Size
        {
            Small_1024 = 1024,
            Med_2048 = 2048,
            Big_4096 = 4096,
            XXL_8192 = 8192,
            super_16K = 16384
        };

        //Ver 3.0
        private Material _matComposite;
        private Material _matScale;

        //Ver 2.0 
        bool readyFlag = false;
        bool capFlag = false;
        int i = 0;

        //int FrameCount = 0;
        int cachedCaptureFramerate = 0;
        ConfigEntry<int> NumberofFrames { get; set; }
        ConfigEntry<Picture_Size> PictureSize { get; set; }
        
        
        /** 对外公开的三个参数，用于video exporter。 */
        ConfigEntry<bool> Alpha { get; set; }

        ConfigEntry<bool> isEnable_3D { get; set; }
        ConfigEntry<int> CaptureWidth { get; set; }
        ConfigEntry<int> CaptureHeight { get; set; }

        ConfigEntry<bool> IsShowMessageOnScreen { get; set; }
        ConfigEntry<int> FrameRate { get; set; }
        ConfigEntry<KeyboardShortcut> KeyCaptureStart { get; set; }
        ConfigEntry<KeyboardShortcut> KeyCaptureCancel { get; set; }
        ConfigEntry<string> OutputPath { get; set; }
        ConfigEntry<FOVangle> Viewangle { get; set; }
        ConfigEntry<float> InterpupillaryDistance { get; set; }
        ConfigEntry<float> StereoConverGencsValue { get; set; }
        //ConfigEntry<float> My_StereoConvergence { get; set; }
        ConfigEntry<int> EyeCubemap_antiAliasing { get; set; }


        //ConfigEntry<ScalingFactor> Scaling_Factor { get; set; }




        void Start()
        {
            Logger.LogInfo("VR360VideoFramesCreator Loaded");
            /***
             * 经过无数次实验，终于完成2.0版。这个版本有以下特点：
             * 1: 去除前作者的假的3d效果。之前的作者并没有真正的实现3d效果，虽然是上下格式，但是根本无法做到3D，瞳距设置再大也没用。
             *    所以我直接去除了。变成了纯2D的360视频模式。
             * 2: 现在还是有点问题，因为缺少一些设置，所以有的特效会无法捕捉。(某一些粒子效果和镜面效果）
             * 3：当图片超过太多，很容易崩程序。应该是内存泄露。需要改进。
             * 4：当前版本主要是能够捕捉少量的图片。
             * 5: 现在开始制作3.0版本。Start From ：2022年11月12日
             * 6: 3.0基本可以用。2022年11月25日
            */

        }
        private void PictureSize_SettingChanged(object sender, EventArgs e)
        {
            CaptureWidth.Value = (int)PictureSize.Value;
            CaptureHeight.Value = (int)PictureSize.Value;
        }
        void Awake()
        {
            NumberofFrames = Config.Bind<int>("Nolonger Useful", "Number of frames", 5, "Nolonger Useful");
            PictureSize = Config.Bind<Picture_Size>("Capture Settings", "PictureSize", Picture_Size.Big_4096, "The value must be no more than 8192 and square of 2");
            FrameRate = Config.Bind<int>("Nolonger Useful", "Framerate", 60,"For the video of 360 degree, The 60 Frames/s will be more smothly.");
            OutputPath = Config.Bind<string>("Nolonger Useful", "Output location", @"D:\output\", "The location of output screenshot");
            Viewangle = Config.Bind<FOVangle>("Capture Settings", "Viewangle", FOVangle.All_360_Degree);
            KeyCaptureStart = Config.Bind<KeyboardShortcut>("Hotkeys", "Key start capture", new KeyboardShortcut(KeyCode.LeftControl, KeyCode.F9), "After pressing this key, capture will start after next leftclick of mouse");
            KeyCaptureCancel = Config.Bind<KeyboardShortcut>("Hotkeys", "Key cancel capture", new KeyboardShortcut(KeyCode.LeftControl, KeyCode.F11), "Cancel the effect of the start key");
            InterpupillaryDistance = Config.Bind<float>("Capture Settings", "Distacne between eyes", 1.2f, "Change it as you like."); 
            StereoConverGencsValue = Config.Bind<float>("Capture Settings", "StereoConver_Value", 0f, "0 for defualt. Change it as you like.");

            //My_StereoConvergence = Config.Bind<float>("Capture Settings", "StereoConvergence of Camera", 60, "Change it as you like.");

            EyeCubemap_antiAliasing = Config.Bind<int>("Nolonger Useful", "antiAliasing_Value", 8, "It should not be change.");
            //Scaling_Factor = Config.Bind<ScalingFactor>("Camera Settings", "Scaling_Factor", ScalingFactor.One, "only one is working.");
            //Logger.LogInfo("VR360VideoFramesCreator Loaded");
            IsShowMessageOnScreen  = Config.Bind<bool>("Z_Others", "Show Message On Screen", false, "Do you want to show message?");
            
            CaptureWidth = Config.Bind<int>("Nolonger Useful", "CaptureWidth_Value", 2560, "Nolonger Useful.");
            CaptureHeight = Config.Bind<int>("Nolonger Useful", "CaptureHeight_Value", 2560, "Nolonger Useful.");
            Alpha = Config.Bind<bool>("Nolonger Useful", "Alpha_Value", false, "Nolonger Useful.");

            isEnable_3D = Config.Bind<bool>("Capture Settings", "isEnable_3D", false, "是否开启3D模式？");

            PictureSize.SettingChanged += new System.EventHandler(this.PictureSize_SettingChanged);
            CaptureWidth.Value =(int) PictureSize.Value;
            CaptureHeight.Value = (int)PictureSize.Value;
            Alpha.Value = false;

            //var ab = AssetBundle.LoadFromMemory(ResourceUtils.GetEmbeddedResource("composite.unity3d"));
            //_matComposite = new Material(ab.LoadAsset<Shader>("composite"));
            //_matScale = new Material(ab.LoadAsset<Shader>("resize"));
            //ab.Unload(false);

            Hooks.Apply();
           
        }
        /// <summary>
        /// Disable built-in screenshots . 
        /// Special thx to bepis. These codes are from there.
        /// </summary>
        private static class Hooks
        {
            public static void Apply()
            {
                var h = Harmony.CreateAndPatchAll(typeof(Hooks), GUID);

                var msvoType = Type.GetType("UnityEngine.Rendering.PostProcessing.MultiScaleVO, Unity.Postprocessing.Runtime");
                h.Patch(AccessTools.Method(msvoType, "PushAllocCommands"), transpiler: new HarmonyMethod(typeof(Hooks), nameof(AoBandingFix)));
            }

#if AI
            // Hook here instead of hooking GameScreenShot.Capture to not affect the Photo functionality
            [HarmonyPrefix, HarmonyPatch(typeof(AIProject.Scene.MapScene), nameof(AIProject.Scene.MapScene.CaptureSS))]
            private static bool CaptureSSOverride() => false;
#elif HS2
            public static bool SoundWasPlayed;

            [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), nameof(GameScreenShot.Capture), typeof(string))]
            private static bool CaptureOverride()
            {
                SoundWasPlayed = true;
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(GameScreenShot), nameof(GameScreenShot.UnityCapture), typeof(string))]
            private static bool CaptureOverride2()
            {
                SoundWasPlayed = true;
                return false;
            }
#endif

            // Separate screenshot class for the studio
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.GameScreenShot), nameof(Studio.GameScreenShot.Capture), typeof(string))]
            private static bool StudioCaptureOverride()
            {
                return false;
            }

            // Fix AO banding in downscaled screenshots
            private static IEnumerable<CodeInstruction> AoBandingFix(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var i in instructions)
                {
                    if (i.opcode == OpCodes.Ldc_I4_S)
                    {
                        if ((int)RenderTextureFormat.RHalf == Convert.ToInt32(i.operand))
                            i.operand = (sbyte)RenderTextureFormat.RFloat;
                        else if ((int)RenderTextureFormat.RGHalf == Convert.ToInt32(i.operand))
                            i.operand = (sbyte)RenderTextureFormat.RGFloat;
                    }
                    yield return i;
                }
            }
        }

        /// <summary>
        /// 2.0
        /// </summary>
        /// <param name="widthLength"></param>
        /// <param name="heightLenth"></param>
        /// <param name="face"></param>
        /// <param name="_prevTextureToRelease"></param>
        /// <param name="antiAliasing_Value"></param>
        /// <param name="scalFac"></param>
        /// <param name="logger_in"></param>
        /// <param name="i_frame"></param>
        /// <returns></returns>
        public static Texture2D CaptureScreen(int widthLength, int heightLenth,
            int face, RenderTexture _prevTextureToRelease, int antiAliasing_Value,
            BepInEx.Logging.ManualLogSource logger_in, int i_frame)
        {
            //logger_in.LogInfo("CaptureScreen 2 level " + sidelength.ToString());
            RenderTexture EyeCubemap = new RenderTexture(widthLength, widthLength, 32, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            if (EyeCubemap.dimension != UnityEngine.Rendering.TextureDimension.Cube)
                EyeCubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            if (EyeCubemap.antiAliasing != antiAliasing_Value)
                EyeCubemap.antiAliasing = antiAliasing_Value;
            //logger_in.LogInfo("i = "+ i_frame + "CaptureScreen 2 level: EyeCubemap new ok");

            RenderTexture EquirectTexture = new RenderTexture(widthLength, widthLength, 32, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            EquirectTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;

            //logger_in.LogInfo("i = "+ i_frame +"CaptureScreen 2 level: EquirectTexture new ok");
            if (Camera.main.name.Equals("MainCamera") == false)
                Camera.main.name = "MainCamera";
            Camera.main.RenderToCubemap(EyeCubemap, face, Camera.MonoOrStereoscopicEye.Mono);
            EyeCubemap.ConvertToEquirect(EquirectTexture, Camera.MonoOrStereoscopicEye.Mono);
            //logger_in.LogInfo("i = "+ i_frame + "CaptureScreen 2 level Mono : RenderToCubemap ConvertToEquirect ok");

            //Camera.main.stereoTargetEye = StereoTargetEyeMask.Right;
            //Camera.main.RenderToCubemap(EyeCubemap, face, Camera.MonoOrStereoscopicEye.Right);
            //EyeCubemap.ConvertToEquirect(EquirectTexture, Camera.MonoOrStereoscopicEye.Right);
            //logger_in.LogInfo("CaptureScreen 2 level Right : RenderToCubemap ConvertToEquirect ok");

            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = EquirectTexture;
            Texture2D tempTexture = new Texture2D(EquirectTexture.width, EquirectTexture.height);
            //logger_in.LogInfo("i = "+ i_frame + "CaptureScreen 2 level 2 Texture2D : new Texture2D ok");

            tempTexture.ReadPixels(new Rect(0, 0, EquirectTexture.width, EquirectTexture.height), 0, 0);
            tempTexture.Apply();
            //logger_in.LogInfo("i = "+ i_frame +"CaptureScreen 2 level ReadPixels : tempTexture ok");
            RenderTexture.active = currentActiveRT;

            EyeCubemap.Release();
            if (_prevTextureToRelease != null)
            {
                _prevTextureToRelease.Release();    //如果上一个保存起来的还没有释放，那么现在可以释放看看的。
                _prevTextureToRelease = null;
            }
            _prevTextureToRelease = EquirectTexture; //先保存起来，不能马上释放，不然会报错。
            return tempTexture;
        }
        public RenderTexture prevTextureToRelease;

        /// <summary>
        /// 2.0
        /// </summary>
        /// <param name="Tex2D"></param>
        /// <param name="Path"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public IEnumerator WritePNG(Texture2D Tex2D, string Path, int i)
        {
            // Exports to a PNG
            string Target = Path + $"output_{i}.png";
            if (Tex2D.isReadable == false)
            {
                Destroy(Tex2D);
            }
            else
            {
                byte[] bytes = Tex2D.EncodeToPNG();
                yield return WriteAsync(bytes, Target); //boxscwei . change by.
                if (prevTextureToRelease != null)
                {
                    prevTextureToRelease.Release();    //如果上一个保存起来的还没有释放，那么现在可以释放看看的。
                    prevTextureToRelease = null;
                }
                if (Tex2D != null)
                    DestroyImmediate(Tex2D);
            }
        }
        /// <summary>
        /// 2.0
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task WriteAsync(byte[] bytes, string filePath)
        {
            try
            {
                await Task.Run(() => {
                    
                    System.IO.File.WriteAllBytes(filePath, bytes);
                    return Task.CompletedTask;
                });
            }
            catch (System.Exception ex) { Debug.LogException(ex); await Task.CompletedTask; }//kills debugger execution loop on exception
            finally { await Task.CompletedTask; }
        }
        /// <summary>
        /// 2.0
        /// </summary>
        /// <returns></returns>
        public IEnumerator CaptureScreenshots()
        {
            yield return new WaitForEndOfFrame();
            //Logger.LogInfo("CaptureScreenshots 1 level i = "+i.ToString());

            StartCoroutine(WritePNG(CaptureScreen(
                (int)PictureSize.Value, (int)PictureSize.Value, (int)Viewangle.Value, prevTextureToRelease,
                (int)EyeCubemap_antiAliasing.Value, Logger, i), OutputPath.Value, i++));
           
        }
        /// <summary>
        /// 2.0
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitForEndOfFrameThen()
        {
            Light[] lights = null;
            int sc = QualitySettings.shadowCascades;
            try
            {
                Logger.LogInfo(i+" for i: WaitForEndOfFrameThen 1 try");
                QualitySettings.shadowCascades = 4;
                lights = FindObjectsOfType<Light>();
                foreach (var l in lights)
                    l.shadowCustomResolution = 8192;
            }
            catch (Exception ex)
            {
                Logger.LogInfo(i + " for i:WaitForEndOfFrameThen 1 try Exception:" + ex.ToString());
            }
            yield return CaptureScreenshots();
            QualitySettings.shadowCascades = sc;
            foreach (var l in lights)
                l.shadowCustomResolution = 0;

        }
        /// <summary>
        /// 3.0
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        private IEnumerator WaitForEndOfFrameThen(Action a)
        {
            var sc = QualitySettings.shadowCascades;

                QualitySettings.shadowCascades = 4;

            var lights = FindObjectsOfType<Light>();
            foreach (var l in lights)
                l.shadowCustomResolution = 8192;
#if TRACE
            LogScreenshotMessage("enter WaitForEndOfFrameThen, starting WaitForEndOfFrame(). ");
#endif
            yield return new WaitForEndOfFrame();
            a();

            QualitySettings.shadowCascades = sc;

            foreach (var l in lights)
                l.shadowCustomResolution = 0;
        }
        /// <summary>
        /// 3.0
        /// </summary>
        /// <param name="alpha"></param>
        private void CaptureAndWrite(bool alpha)
        {
            //alpha is always false here
            Config.Reload();
            CaptureWidth.Value = (int)PictureSize.Value;
            CaptureHeight.Value = (int)PictureSize.Value;
#if TRACE
            LogScreenshotMessage("enter CaptureAndWrite, Config.Reload()...ok ");
#endif
            StartCoroutine(WriteTex(Capture((int)PictureSize.Value, (int)PictureSize.Value, 1, alpha), alpha));
        }
        /// <summary>
        /// 3.0 Capture the screen into a texture based on supplied arguments. Remember to RenderTexture.ReleaseTemporary the texture when done with it.
        /// </summary>
        /// <param name="width">Width of the resulting capture, after downscaling</param>
        /// <param name="height">Height of the resulting capture, after downscaling</param>
        /// <param name="downscaling">How much to oversize and then downscale. 1 for none.</param>
        /// <param name="transparent">Should the capture be transparent</param>
        public RenderTexture Capture(int width, int height, int downscaling, bool transparent)
        {
            try { OnPreCapture?.Invoke(); }
            catch (Exception ex) { Logger.LogError(ex); }

            try
            {
#if TRACE
                LogScreenshotMessage("enter Capture, startr CaptureOpaque...ok ");
#endif
                return CaptureOpaque(width, height, downscaling);
                
            }
            finally
            {
                try { OnPostCapture?.Invoke(); }
                catch (Exception ex) { Logger.LogError(ex); }
#if TRACE
                LogScreenshotMessage("CaptureOpaque...Done. ");
#endif
            }
        }
        //4.0 for true 3D manually
        private RenderTexture CaptureOpaque(int width, int height, int downscaling)
        {
            var scaledWidth = width;
            var scaledHeight = height;

            //4.0 get the transform info and comput the right eye's transform
#if TRACE
            if (Camera.main.stereoEnabled == true)
                LogScreenshotMessage("StereoEnabled is Ture!! Yes!!");
            else
                LogScreenshotMessage("StereoEnabled is False!! No!!");
#endif
            var cam = Camera.main.gameObject;
            var dof = cam.GetComponent<UnityStandardAssets.ImageEffects.DepthOfField>();
            float dofPrevBlurSize = 0;
            if (dof != null)
            {
                dofPrevBlurSize = dof.maxBlurSize;
                var ratio = Screen.height / (float)scaledHeight; //Use larger of width/height?
                dof.maxBlurSize *= ratio;
            }
#if TRACE
            LogScreenshotMessage("enter CaptureOpaque, startr CaptureScreen...ok ");
#endif
            var colour = CaptureScreen(scaledWidth, scaledHeight, this);
#if TRACE
            LogScreenshotMessage("CaptureScreen...Done. ");
#endif

            if (dof != null)
            {
                dof.maxBlurSize = dofPrevBlurSize;
            }

            return colour;
        }
        private static IEnumerable<AmbientOcclusion> DisableAmbientOcclusion()
        {
            var aos = new List<AmbientOcclusion>();

            if (true)
                foreach (var vol in FindObjectsOfType<PostProcessVolume>())
                {
                    if (vol.profile.TryGetSettings(out AmbientOcclusion ao))
                    {
                        if (!ao.enabled.value) continue;
                        ao.enabled.value = false;
                        aos.Add(ao);
                    }
                }

            return aos;
        }
        private static string GetCaptureFilename()
        {
            var dir = Path.Combine(Paths.GameRootPath, "UserData", "cap");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir,
                $"HS2_VR360_{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}.png"
                );
        }
        private IEnumerator WriteTex(RenderTexture rt, bool alpha)
        {
            //Pull texture off of GPU
            var req = AsyncGPUReadback.Request(rt, 0, 0, rt.width, 0, rt.height, 0, 1, alpha ? TextureFormat.RGBA32 : TextureFormat.RGBAFloat);
            while (!req.done) yield return null;

            RenderTexture.ReleaseTemporary(rt);
            string path = GetCaptureFilename();

            LogScreenshotMessage("Writing rendered screenshot to " + path.Substring(Paths.GameRootPath.Length));

            //Write raw pixel data to a file
            //Uses pngcs Unity fork: https://github.com/andrew-raphael-lukasik/pngcs
           
                using (var buffer = req.GetData<Color>())
                    yield return PNG.WriteAsync(buffer.ToArray(), req.width, req.height, 8, false, false, path);
        }
        /// <summary>
        /// 3.0
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="A"></param>
        /// <returns></returns>
        private static RenderTexture CaptureScreen(int width, int height, VR360VideoFramesCreator A)
        {
            // Setup postprocessing effects to work with the capture
            var aos = DisableAmbientOcclusion();
#if TRACE
            A.LogScreenshotMessage("enter CaptureScreen, startr RenderTexture.GetTemporary...ok ");
#endif
            // Do the capture
            var fmt = RenderTextureFormat.Default;
            int height2 = height;
            if (A.isEnable_3D.Value == false)
            {
                height2 = (int)(height / 2);
            }
                var rt = RenderTexture.GetTemporary(width, height2, 32, fmt, RenderTextureReadWrite.Default);
            if (rt.dimension != TextureDimension.Tex2D)
                rt.dimension = TextureDimension.Tex2D;
            var eyecudemap = new RenderTexture(width, height, 32, fmt, RenderTextureReadWrite.Default);
            if (eyecudemap.dimension != UnityEngine.Rendering.TextureDimension.Cube)
                eyecudemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            if (eyecudemap.antiAliasing != 8)
                eyecudemap.antiAliasing = 8; 

            var cam = Camera.main;

            var oldCf = cam.clearFlags;
            var oldBg = cam.backgroundColor;
            var oldRt = cam.targetTexture;
            var oldRtc = Camera.current.targetTexture;

            cam.clearFlags = oldCf;
            cam.backgroundColor =oldBg;
            cam.targetTexture = eyecudemap;


            if (A.isEnable_3D.Value == false)
            {
                cam.RenderToCubemap(eyecudemap, (int)A.Viewangle.Value, Camera.MonoOrStereoscopicEye.Mono);
                eyecudemap.ConvertToEquirect(rt, Camera.MonoOrStereoscopicEye.Mono);

            }
            else
            {
                cam.stereoSeparation =(float) A.InterpupillaryDistance.Value;
                float converValue = (float)A.StereoConverGencsValue.Value;
                if(converValue>0)
                    cam.stereoConvergence= converValue;
                cam.stereoTargetEye = StereoTargetEyeMask.Both;
                cam.RenderToCubemap(eyecudemap, (int)A.Viewangle.Value, Camera.MonoOrStereoscopicEye.Left);
                cam.RenderToCubemap(eyecudemap, (int)A.Viewangle.Value, Camera.MonoOrStereoscopicEye.Right);
                eyecudemap.ConvertToEquirect(rt, Camera.MonoOrStereoscopicEye.Left);
                eyecudemap.ConvertToEquirect(rt, Camera.MonoOrStereoscopicEye.Right);

            }

            cam.clearFlags = oldCf;
            cam.backgroundColor = oldBg;
            cam.targetTexture = oldRt;
            Camera.current.targetTexture = oldRtc;

            eyecudemap.Release();
            

            // Restore postprocessing settings
            int Downscaling_Value = 1;
            if (Downscaling_Value == 1)
                foreach (var ao in aos)
                    ao.enabled.value = true;

            return rt;
        }
        private void LogScreenshotMessage(string text)
        {
            bool isShowMessage =(bool) IsShowMessageOnScreen.Value;
            if (isShowMessage)
                Logger.LogMessage(text);
            else
                Logger.LogInfo(text);
        }
        public void Update()
        {
            //3.0
            if (KeyCaptureStart.Value.IsDown())
            {
#if TRACE
                LogScreenshotMessage("start cap.");
#endif
                StartCoroutine(WaitForEndOfFrameThen(() => CaptureAndWrite(false)));
            }
            /**
             * ver 2.0
            if (KeyCaptureStart.Value.IsDown())
            {
                readyFlag = true;
            }
            if (KeyCaptureCancel.Value.IsDown())
            {
                readyFlag = false;
            }
            if (Input.GetMouseButtonDown(0) && readyFlag)
            {
                capFlag = true;
                readyFlag = false;
                cachedCaptureFramerate = Time.captureFramerate;
                Time.captureFramerate = FrameRate.Value;
            }
            if ((i < NumberofFrames.Value) && capFlag)
            {
                Logger.LogInfo(i + " for i: start.");
                StartCoroutine(WaitForEndOfFrameThen());
                
            }
            if (i >= NumberofFrames.Value)
            {
                i = 0;
                capFlag = false;

                Time.captureFramerate = cachedCaptureFramerate;
                cachedCaptureFramerate = 0;
            }
            */
        }
    }
}
