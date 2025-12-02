using HKLib.hk2018;
using HKLib.Serialization.hk2018.Binary;
using System.Numerics;
using IONET;
using IONET.Core;
using IONET.Core.Animation;
using IONET.Core.Model;
using IONET.Core.Skeleton;
using System.Diagnostics;

class Program
{
    enum ExportFormat
    {
        DAE,
        GLTF
    }

    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            PrintUsage();
            return;
        }

        string skeletonPath = args[0];
        string animationPathOrFolder = args[1];
        string? outputFolder = null;
        ExportFormat exportFormat = ExportFormat.GLTF; // Default to GLTF

        // Parse optional flags
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "-o" && i + 1 < args.Length)
            {
                outputFolder = args[i + 1];
                i++; // Skip next argument as it's the output folder path
            }
            else if (args[i] == "-gltf")
            {
                exportFormat = ExportFormat.GLTF;
            }
            else if (args[i] == "-dae")
            {
                exportFormat = ExportFormat.DAE;
            }
        }

        // Validate skeleton file exists
        if (!File.Exists(skeletonPath))
        {
            Console.WriteLine($"Error: Skeleton file not found: {skeletonPath}");
            return;
        }

        // Load skeleton once
        Console.WriteLine($"Loading skeleton from: {skeletonPath}");
        var skeleton = LoadSkeleton(skeletonPath);
        if (skeleton == null)
        {
            Console.WriteLine("Error: Failed to load skeleton.");
            return;
        }

        // Determine if input is a file or folder
        List<string> animationFiles = new List<string>();
        string? inputBaseFolder = null;
        
        if (Directory.Exists(animationPathOrFolder))
        {
            // Recursive search for .anmb files
            Console.WriteLine($"Searching for .anmb files in: {animationPathOrFolder}");
            animationFiles = Directory.GetFiles(animationPathOrFolder, "*.anmb", SearchOption.AllDirectories).ToList();
            Console.WriteLine($"Found {animationFiles.Count} animation file(s)");
            inputBaseFolder = animationPathOrFolder;
        }
        else if (File.Exists(animationPathOrFolder))
        {
            // Single file
            animationFiles.Add(animationPathOrFolder);
            inputBaseFolder = Path.GetDirectoryName(animationPathOrFolder);
        }
        else
        {
            Console.WriteLine($"Error: Animation path not found: {animationPathOrFolder}");
            return;
        }

        if (animationFiles.Count == 0)
        {
            Console.WriteLine("No animation files found.");
            return;
        }

        Console.WriteLine($"Export format: {exportFormat}");
        Console.WriteLine($"Output folder: {outputFolder ?? "same as input"}");

        // Process each animation file
        foreach (var animFile in animationFiles)
        {
            ProcessAnimation(animFile, skeleton, outputFolder, exportFormat, inputBaseFolder);
        }

        Console.WriteLine("All animations processed successfully.");
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  AnimationConverter <skeleton_file> <animation_file_or_folder> [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  skeleton_file            Path to the skeleton file (.skl) [Required]");
        Console.WriteLine("  animation_file_or_folder   Path to a single animation file (.anmb) or folder containing .anmb files [Required]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o <output_folder>         Output folder for exported files [Optional, default: same folder as input animation]");
        Console.WriteLine("  -gltf      Export as GLTF format (default)");
        Console.WriteLine("  -dae              Export as Collada DAE format");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  AnimationConverter skeleton.skl animation.anmb");
        Console.WriteLine("  AnimationConverter skeleton.skl C:\\animations -gltf");
        Console.WriteLine("  AnimationConverter skeleton.skl animation.anmb -o C:\\output -dae");
        Console.WriteLine("  AnimationConverter skeleton.skl C:\\animations -o C:\\output -gltf");
    }

    static hkaSkeleton? LoadSkeleton(string skeletonPath)
    {
        try
        {
            var serializer = new HavokBinarySerializer();
            IEnumerable<IHavokObject> havokObjects = serializer.ReadAllObjects(skeletonPath);
            var skeletons = havokObjects.OfType<hkaSkeleton>().ToList();
            return skeletons.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading skeleton: {ex.Message}");
            return null;
        }
    }

    static void ProcessAnimation(string animationPath, hkaSkeleton skeleton, string? outputFolder, ExportFormat exportFormat, string? inputBaseFolder = null)
    {
        try
        {
            Console.WriteLine($"Processing: {animationPath}");

            var serializer = new HavokBinarySerializer();
            IEnumerable<IHavokObject> havokObjects = serializer.ReadAllObjects(animationPath);
       
            var animations = havokObjects.OfType<hkaAnimation>().ToList();
  var animation = animations.FirstOrDefault();
 
        if (animation == null)
            {
         Console.WriteLine($"  Warning: No animation found in {animationPath}");
        return;
            }

            animation.setSkeleton(skeleton);

            var animationBindings = havokObjects.OfType<hkaAnimationBinding>().ToList();
            var animationBinding = animationBindings.FirstOrDefault();

  if (animationBinding == null)
        {
                Console.WriteLine($"  Warning: No animation binding found in {animationPath}");
 return;
     }

      var allTracks = animation.fetchAllTracks();

        // Get file name without extension for animation name
          string animationName = Path.GetFileNameWithoutExtension(animationPath);
  var scene = BuildSimpleSkinnedScene(skeleton, animationBinding, allTracks, exportFormat, animationName);

            // Generate output path preserving folder structure
        string fileName = Path.GetFileNameWithoutExtension(animationPath);
            // Default to the animation file's directory if no output folder specified
   string outputDir = outputFolder ?? Path.GetDirectoryName(animationPath) ?? Directory.GetCurrentDirectory();
            
            // Preserve folder structure if processing from a folder
      if (outputFolder != null && inputBaseFolder != null)
            {
     string? animDirectory = Path.GetDirectoryName(animationPath);
     if (animDirectory != null)
                {
        string relativePath = Path.GetRelativePath(inputBaseFolder, animDirectory);
     if (relativePath != ".")
           {
      outputDir = Path.Combine(outputFolder, relativePath);
      }
    }
     }
     
     // Create output directory if it doesn't exist
         if (!Directory.Exists(outputDir))
{
           Directory.CreateDirectory(outputDir);
        }

       string fileExtension = exportFormat == ExportFormat.GLTF ? ".gltf" : ".dae";
            string outputPath = Path.Combine(outputDir, $"{fileName}{fileExtension}");

            IOManager.ExportScene(scene, outputPath, new ExportSettings
            {
ExportAnimations = true,
        FrameRate = 30.0f,
 BlenderMode = true
          });

            Console.WriteLine($"  Exported: {outputPath}");
        }
    catch (Exception ex)
      {
  Console.WriteLine($"  Error processing {animationPath}: {ex.Message}");
        }
    }

    private static IOScene BuildSimpleSkinnedScene(hkaSkeleton khaSkeleton, hkaAnimationBinding hkaAnimationBinding, List<List<hkQsTransform>> allFrames, ExportFormat exportFormat, string animationName)
    {
        List<IOBone> bones = new List<IOBone>();
        for (int i = 0; i < khaSkeleton.m_bones.Count; i++)
        {
            var bone = khaSkeleton.m_bones[i];

            var normalizedRotation = Quaternion.Normalize(khaSkeleton.m_referencePose[i].m_rotation);
            var outputBone = new IOBone
            {
                Name = bone.m_name,
                Translation = new Vector3(
                    khaSkeleton.m_referencePose[i].m_translation.X,
                    khaSkeleton.m_referencePose[i].m_translation.Y,
                    khaSkeleton.m_referencePose[i].m_translation.Z),
                Rotation = new Quaternion(
                    normalizedRotation.X,
                    normalizedRotation.Y,
                    normalizedRotation.Z,
                    normalizedRotation.W),
                Scale = Vector3.One // Havok does not store scale in reference pose
            };

            bones.Insert(i, outputBone);
        }

        var rootBone = null as IOBone;
        // Establish parent-child relationships
        var boneList = bones.ToList();
        for (int i = 0; i < khaSkeleton.m_bones.Count; i++)
        {
            var boneParentIndex = khaSkeleton.m_parentIndices[i];

            if (boneParentIndex < 0)
            {
                rootBone = boneList[i];
            }

            if (boneParentIndex >= 0)
            {
                boneList[i].Parent = boneList[boneParentIndex];
            }
        }

        var skeleton = new IOSkeleton();
        skeleton.RootBones.Add(rootBone);

        // Model with only skeleton (no meshes)
        var model = new IOModel { Name = "Model", Skeleton = skeleton };
        // Ensure no mesh is exported
        model.Meshes.Clear();

        int numFrames = allFrames.Count;
        var sceneAnim = new IOAnimation { Name = animationName, StartFrame = 0, EndFrame = numFrames - 1 };
        
        for (int i = 0; i < hkaAnimationBinding.m_transformTrackToBoneIndices.Count; i++)
        {
            int boneIndex = hkaAnimationBinding.m_transformTrackToBoneIndices[i];
            string boneName = khaSkeleton.m_bones[boneIndex].m_name;
            var boneGroup = new IOAnimation { Name = boneName };
            var posXTrack = new IOAnimationTrack(IOAnimationTrackType.PositionX);
            var posYTrack = new IOAnimationTrack(IOAnimationTrackType.PositionY);
            var posZTrack = new IOAnimationTrack(IOAnimationTrackType.PositionZ);
            var scaleXTrack = new IOAnimationTrack(IOAnimationTrackType.ScaleX);
            var scaleYTrack = new IOAnimationTrack(IOAnimationTrackType.ScaleY);
            var scaleZTrack = new IOAnimationTrack(IOAnimationTrackType.ScaleZ);

            // Process all frames for this bone
            for (int f = 0; f < numFrames; f++)
            {
                hkQsTransform transform = allFrames[f][i];

                posXTrack.InsertKeyframe(f, transform.m_translation.X);
                posYTrack.InsertKeyframe(f, transform.m_translation.Y);
                posZTrack.InsertKeyframe(f, transform.m_translation.Z);

                scaleXTrack.InsertKeyframe(f, transform.m_scale.X);
                scaleYTrack.InsertKeyframe(f, transform.m_scale.Y);
                scaleZTrack.InsertKeyframe(f, transform.m_scale.Z);
            }

            boneGroup.Tracks.Add(posXTrack);
            boneGroup.Tracks.Add(posYTrack);
            boneGroup.Tracks.Add(posZTrack);
            boneGroup.Tracks.Add(scaleXTrack);
            boneGroup.Tracks.Add(scaleYTrack);
            boneGroup.Tracks.Add(scaleZTrack);

            // Add rotation tracks based on export format
            if (exportFormat == ExportFormat.DAE)
            {
                // DAE mode: use Euler rotation tracks
                var rotXTrack = new IOAnimationTrack(IOAnimationTrackType.RotationEulerX);
                var rotYTrack = new IOAnimationTrack(IOAnimationTrackType.RotationEulerY);
                var rotZTrack = new IOAnimationTrack(IOAnimationTrackType.RotationEulerZ);

                for (int f = 0; f < numFrames; f++)
                {
                    hkQsTransform transform = allFrames[f][i];

                    // Convert quaternion to Euler angles (XYZ rotation order)
                    float sinr_cosp = 2.0f * (transform.m_rotation.W * transform.m_rotation.X + transform.m_rotation.Y * transform.m_rotation.Z);
                    float cosr_cosp = 1.0f - 2.0f * (transform.m_rotation.X * transform.m_rotation.X + transform.m_rotation.Y * transform.m_rotation.Y);
                    float angleX = MathF.Atan2(sinr_cosp, cosr_cosp);

                    float sinp = 2.0f * (transform.m_rotation.W * transform.m_rotation.Y - transform.m_rotation.Z * transform.m_rotation.X);
                    float angleY = MathF.Abs(sinp) >= 1.0f ? MathF.CopySign(MathF.PI / 2.0f, sinp) : MathF.Asin(sinp);

                    float siny_cosp = 2.0f * (transform.m_rotation.W * transform.m_rotation.Z + transform.m_rotation.X * transform.m_rotation.Y);
                    float cosy_cosp = 1.0f - 2.0f * (transform.m_rotation.Y * transform.m_rotation.Y + transform.m_rotation.Z * transform.m_rotation.Z);
                    float angleZ = MathF.Atan2(siny_cosp, cosy_cosp);

                    // TODO: dae output need to swap x and z rotation?
                    rotXTrack.InsertKeyframe(f, angleZ);
                    rotYTrack.InsertKeyframe(f, angleY);
                    rotZTrack.InsertKeyframe(f, angleX);
                }

                boneGroup.Tracks.Add(rotZTrack);
                boneGroup.Tracks.Add(rotYTrack);
                boneGroup.Tracks.Add(rotXTrack);
            }
            else // GLTF mode
            {
                // GLTF mode: use quaternion tracks
                var quatXTrack = new IOAnimationTrack(IOAnimationTrackType.QuatX);
                var quatYTrack = new IOAnimationTrack(IOAnimationTrackType.QuatY);
                var quatZTrack = new IOAnimationTrack(IOAnimationTrackType.QuatZ);
                var quatWTrack = new IOAnimationTrack(IOAnimationTrackType.QuatW);

                for (int f = 0; f < numFrames; f++)
                {
                    hkQsTransform transform = allFrames[f][i];

                    quatXTrack.InsertKeyframe(f, transform.m_rotation.X);
                    quatYTrack.InsertKeyframe(f, transform.m_rotation.Y);
                    quatZTrack.InsertKeyframe(f, transform.m_rotation.Z);
                    quatWTrack.InsertKeyframe(f, transform.m_rotation.W);
                }

                boneGroup.Tracks.Add(quatXTrack);
                boneGroup.Tracks.Add(quatYTrack);
                boneGroup.Tracks.Add(quatZTrack);
                boneGroup.Tracks.Add(quatWTrack);
            }

            sceneAnim.Groups.Add(boneGroup);
        }

        // Scene with rig + animation only
        var scene = new IOScene { Name = "Scene" };
        scene.Models.Add(model);
        scene.Animations.Add(sceneAnim);

        return scene;
    }
}
