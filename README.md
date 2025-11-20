# FF16 Animation Converter

A tool to convert Final Fantasy XVI `.anmb` animation files into `.gltf` or `.dae` format.

## Note
⚠️ **The animation needs to be played at 30 FPS.** Playing at 24 FPS will cause some joints to have flipping rotation issues.

## Usage

### Convert Single File
```bash
AnimationConverter.exe <skeleton_file> <animation_file>
```

**Example:**
```bash
AnimationConverter.exe "D:\FFXVIOut\chara\c1001\pack\c1001.extracted\animation\chara\c1001\skeleton\body\body.skl" "D:\FFXVIOut\animation\chara\c1001\animation\a0001\common\access_torgal\touch_torgal_a1.anmb"
```

### Convert All Files in Folder
```bash
AnimationConverter.exe <skeleton_file> <animation_folder>
```

**Example:**
```bash
AnimationConverter.exe "D:\FFXVIOut\chara\c1001\pack\c1001.extracted\animation\chara\c1001\skeleton\body\body.skl" "D:\FFXVIOut\animation\chara\c1001\animation\a0001\common\"
```

### Custom Output Directory
By default, output files are saved in the same folder as the original `.anmb` file. Use the `-o` flag to specify a custom output directory:

```bash
AnimationConverter.exe <skeleton_file> <animation_input> -o <output_folder>
```

**Example:**
```bash
AnimationConverter.exe "D:\FFXVIOut\chara\c1001\pack\c1001.extracted\animation\chara\c1001\skeleton\body\body.skl" "D:\FFXVIOut\animation\chara\c1001\animation\a0001\common\" -o "D:\temp\output"
```

### Output as dae
```bash
AnimationConverter.exe <skeleton_file> <animation_folder> -dae
```

By default, the animation will be exported as gltf, add -dae if you want dae.
For importing into blender, it need to be gltf. 



## Parameters

| Parameter | Description |
|-----------|-------------|
| `<skeleton_file>` | Path to the skeleton file (`.skl`) |
| `<animation_file>` | Path to a single animation file (`.anmb`) |
| `<animation_folder>` | Path to a folder containing animation files |
| `-o <output_folder>` | Optional: Custom output directory |
| `-dae` | Optional: Output as dae |
| `-gltf` | Optional: Output as gltf|