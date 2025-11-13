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

        // Parse optional -o flag
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "-o" && i + 1 < args.Length)
            {
                outputFolder = args[i + 1];
                i++; // Skip next argument as it's the output folder path
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
      string baseInputPath;
        
        if (Directory.Exists(animationPathOrFolder))
        {
            // Recursive search for .anmb files
            Console.WriteLine($"Searching for .anmb files in: {animationPathOrFolder}");
     animationFiles = Directory.GetFiles(animationPathOrFolder, "*.anmb", SearchOption.AllDirectories).ToList();
            Console.WriteLine($"Found {animationFiles.Count} animation file(s)");
            baseInputPath = animationPathOrFolder;
 }
        else if (File.Exists(animationPathOrFolder))
    {
     // Single file
       animationFiles.Add(animationPathOrFolder);
            baseInputPath = Path.GetDirectoryName(animationPathOrFolder) ?? Directory.GetCurrentDirectory();
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

  // Process each animation file
 foreach (var animFile in animationFiles)
        {
  ProcessAnimation(animFile, skeleton, outputFolder, baseInputPath);
        }

        Console.WriteLine("All animations processed successfully.");
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  AnimationConverter <skeleton_file> <animation_file_or_folder> [-o <output_folder>]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  skeleton_file              Path to the skeleton file (.skl) [Required]");
        Console.WriteLine("  animation_file_or_folder   Path to a single animation file (.anmb) or folder containing .anmb files [Required]");
        Console.WriteLine("  -o <output_folder>         Output folder for exported files [Optional, default: current directory]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  AnimationConverter skeleton.skl animation.anmb");
        Console.WriteLine("  AnimationConverter skeleton.skl C:\\animations");
        Console.WriteLine("  AnimationConverter skeleton.skl animation.anmb -o C:\\output");
        Console.WriteLine("  AnimationConverter skeleton.skl C:\\animations -o C:\\output");
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

    static void ProcessAnimation(string animationPath, hkaSkeleton skeleton, string? outputFolder, string baseInputPath)
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

            var scene = BuildSimpleSkinnedScene(skeleton, animationBinding, allTracks);

      // Generate output path preserving folder structure
            string fileName = Path.GetFileNameWithoutExtension(animationPath);
            string outputDir;
       
   if (outputFolder != null)
     {
                // Get relative path from base input to current animation file
  string animDir = Path.GetDirectoryName(animationPath) ?? string.Empty;
 string relativePath = Path.GetRelativePath(baseInputPath, animDir);
  
   // Combine output folder with relative path
     outputDir = Path.Combine(outputFolder, relativePath);
            }
            else
            {
  // If no output folder specified, use the same directory as the animation file
     outputDir = Path.GetDirectoryName(animationPath) ?? Directory.GetCurrentDirectory();
            }
            
            // Create output directory if it doesn't exist
      if (!Directory.Exists(outputDir))
         {
         Directory.CreateDirectory(outputDir);
            }

            string outputPath = Path.Combine(outputDir, $"{fileName}.dae");

  IOManager.ExportScene(scene, outputPath, new ExportSettings
        {
    ExportAnimations = true,
    FrameRate = 30.0f,
         BlenderMode = true
     });

   Console.WriteLine($"Exported: {outputPath}");
        }
    catch (Exception ex)
        {
      Console.WriteLine($"  Error processing {animationPath}: {ex.Message}");
  }
    }

    private static IOScene BuildSimpleSkinnedScene(hkaSkeleton khaSkeleton, hkaAnimationBinding hkaAnimationBinding, Dictionary<int, List<hkQsTransform>> poseAtTime)
    {
        List<IOBone> bones = new List<IOBone>();
        for (int i = 0; i < khaSkeleton.m_bones.Count; i++)
        {
            var bone = khaSkeleton.m_bones[i];
            //Console.WriteLine($"Bone {i}: {bone.m_name}");
            //Debug.WriteLine($"Bone {i}: {bone.m_name}");

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


        var sceneAnim = new IOAnimation { Name = "Anm", StartFrame = 0, EndFrame = poseAtTime[hkaAnimationBinding.m_transformTrackToBoneIndices[0]].Count - 1 };
        for (int i = 0; i < hkaAnimationBinding.m_transformTrackToBoneIndices.Count; i++)
        {
            int boneIndex = hkaAnimationBinding.m_transformTrackToBoneIndices[i];
            string boneName = khaSkeleton.m_bones[boneIndex].m_name;
            var boneGroup = new IOAnimation { Name = boneName };
            var posXTrack = new IOAnimationTrack(IOAnimationTrackType.PositionX);
            var posYTrack = new IOAnimationTrack(IOAnimationTrackType.PositionY);
            var posZTrack = new IOAnimationTrack(IOAnimationTrackType.PositionZ);
            var rotXTrack = new IOAnimationTrack(IOAnimationTrackType.RotationEulerX);
            var rotYTrack = new IOAnimationTrack(IOAnimationTrackType.RotationEulerY);
            var rotZTrack = new IOAnimationTrack(IOAnimationTrackType.RotationEulerZ);
            var scaleXTrack = new IOAnimationTrack(IOAnimationTrackType.ScaleX);
            var scaleYTrack = new IOAnimationTrack(IOAnimationTrackType.ScaleY);
            var scaleZTrack = new IOAnimationTrack(IOAnimationTrackType.ScaleZ);

            if (!poseAtTime.ContainsKey(boneIndex))
                continue;

            var poseFrames = poseAtTime[boneIndex];
            for (int f = 0; f < poseFrames.Count; f++)
            {
                int frame = f;
                hkQsTransform transform = poseFrames[f];


                float posX = transform.m_translation.X;
                float posY = transform.m_translation.Y;
                float posZ = transform.m_translation.Z;
                posXTrack.InsertKeyframe(frame, posX);
                posYTrack.InsertKeyframe(frame, posY);
                posZTrack.InsertKeyframe(frame, posZ);

                // Convert quaternion to Euler angles (XYZ rotation order)
                // Reference: https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles

                float sinr_cosp = 2.0f * (transform.m_rotation.W * transform.m_rotation.X + transform.m_rotation.Y * transform.m_rotation.Z);
                float cosr_cosp = 1.0f - 2.0f * (transform.m_rotation.X * transform.m_rotation.X + transform.m_rotation.Y * transform.m_rotation.Y);
                float angleX = MathF.Atan2(sinr_cosp, cosr_cosp);

                // Convert quaternion to Euler Y
                float sinp = 2.0f * (transform.m_rotation.W * transform.m_rotation.Y - transform.m_rotation.Z * transform.m_rotation.X);
                float angleY;
                if (MathF.Abs(sinp) >= 1.0f)
                    angleY = MathF.CopySign(MathF.PI / 2.0f, sinp); // use 90 degrees if out of range
                else
                    angleY = MathF.Asin(sinp);

                // Convert quaternion to Euler Z
                float siny_cosp = 2.0f * (transform.m_rotation.W * transform.m_rotation.Z + transform.m_rotation.X * transform.m_rotation.Y);
                float cosy_cosp = 1.0f - 2.0f * (transform.m_rotation.Y * transform.m_rotation.Y + transform.m_rotation.Z * transform.m_rotation.Z);
                float angleZ = MathF.Atan2(siny_cosp, cosy_cosp);

                // TODO: dae output need to swap x and z rotation?
                rotXTrack.InsertKeyframe(frame, angleZ);
                rotYTrack.InsertKeyframe(frame, angleY);
                rotZTrack.InsertKeyframe(frame, angleX);

                float scaleX = transform.m_scale.X;
                float scaleY = transform.m_scale.Y;
                float scaleZ = transform.m_scale.Z;
                scaleXTrack.InsertKeyframe(frame, scaleX);
                scaleYTrack.InsertKeyframe(frame, scaleY);
                scaleZTrack.InsertKeyframe(frame, scaleZ);
            }
            boneGroup.Tracks.Add(posXTrack);
            boneGroup.Tracks.Add(posYTrack);
            boneGroup.Tracks.Add(posZTrack);
            boneGroup.Tracks.Add(scaleXTrack);
            boneGroup.Tracks.Add(scaleYTrack);
            boneGroup.Tracks.Add(scaleZTrack);
            boneGroup.Tracks.Add(rotZTrack);
            boneGroup.Tracks.Add(rotYTrack);
            boneGroup.Tracks.Add(rotXTrack);
            sceneAnim.Groups.Add(boneGroup);
        }

        // Scene with rig + animation only
        var scene = new IOScene { Name = "Scene" };
        scene.Models.Add(model);
        scene.Animations.Add(sceneAnim);

        return scene;
    }
}
