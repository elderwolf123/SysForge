using System;
using System.IO;
using System.Text;

namespace RamOptimizer.Compression.Tests
{
    /// <summary>
    /// Generates realistic test data for compression testing
    /// </summary>
    public class TestDataGenerator
    {
        /// <summary>
        /// Create a dummy game folder for testing
        /// </summary>
        public static string CreateDummyGameFolder(string basePath, string gameName, int sizeMB = 100)
        {
            string gamePath = Path.Combine(basePath, gameName);
            Directory.CreateDirectory(gamePath);

            // Create realistic game structure
            CreateTextureFolder(gamePath, sizeMB / 4);
            CreateAudioFolder(gamePath, sizeMB / 4);
            CreateScriptFolder(gamePath, sizeMB / 8);
            CreateShaderFolder(gamePath, sizeMB / 8);
            CreateExecutableFiles(gamePath, sizeMB / 4);
            CreateConfigFiles(gamePath);

            return gamePath;
        }

        private static void CreateTextureFolder(string gamePath, int sizeMB)
        {
            string texturesPath = Path.Combine(gamePath, "Data", "Textures");
            Directory.CreateDirectory(texturesPath);

            // Create DDS files (semi-compressed texture format)
            int fileCount = Math.Max(5, sizeMB / 10);
            int fileSizeBytes = (sizeMB * 1024 * 1024) / fileCount;

            for (int i = 0; i < fileCount; i++)
            {
                string fileName = $"texture_{i:D4}.dds";
                CreateRandomFile(Path.Combine(texturesPath, fileName), fileSizeBytes);
            }
        }

        private static void CreateAudioFolder(string gamePath, int sizeMB)
        {
            string audioPath = Path.Combine(gamePath, "Data", "Audio");
            Directory.CreateDirectory(audioPath);

            // Create WAV files (uncompressed audio)
            int fileCount = Math.Max(3, sizeMB / 15);
            int fileSizeBytes = (sizeMB * 1024 * 1024) / fileCount;

            for (int i = 0; i < fileCount; i++)
            {
                string fileName = $"sound_{i:D3}.wav";
                CreatePatternedFile(Path.Combine(audioPath, fileName), fileSizeBytes, 0xAA); // Patterned for better compression
            }
        }

        private static void CreateScriptFolder(string gamePath, int sizeMB)
        {
            string scriptsPath = Path.Combine(gamePath, "Scripts");
            Directory.CreateDirectory(scriptsPath);

            // Create script files (highly compressible text)
            int fileCount = Math.Max(10, sizeMB / 5);
            int fileSizeBytes = (sizeMB * 1024 * 1024) / fileCount;

            for (int i = 0; i < fileCount; i++)
            {
                string fileName = $"script_{i:D3}.lua";
                CreateScriptFile(Path.Combine(scriptsPath, fileName), fileSizeBytes);
            }
        }

        private static void CreateShaderFolder(string gamePath, int sizeMB)
        {
            string shadersPath = Path.Combine(gamePath, "Shaders");
            Directory.CreateDirectory(shadersPath);

            // Create shader files (text-based, compressible)
            int fileCount = Math.Max(5, sizeMB / 10);
            int fileSizeBytes = (sizeMB * 1024 * 1024) / fileCount;

            for (int i = 0; i < fileCount; i++)
            {
                string fileName = $"shader_{i:D2}.glsl";
                CreateShaderFile(Path.Combine(shadersPath, fileName), fileSizeBytes);
            }
        }

        private static void CreateExecutableFiles(string gamePath, int sizeMB)
        {
            // Create executable (binary, low compressibility)
            CreateRandomFile(Path.Combine(gamePath, "Game.exe"), sizeMB * 512 * 1024);
            CreateRandomFile(Path.Combine(gamePath, "GameEngine.dll"), sizeMB * 256 * 1024);
        }

        private static void CreateConfigFiles(string gamePath)
        {
            // Create config files (highly compressible)
            File.WriteAllText(Path.Combine(gamePath, "config.ini"), GenerateConfigContent(5000));
            File.WriteAllText(Path.Combine(gamePath, "settings.json"), GenerateJsonContent(3000));
            File.WriteAllText(Path.Combine(gamePath, "readme.txt"), GenerateReadmeContent(2000));
        }

        private static void CreateRandomFile(string path, int sizeBytes)
        {
            var random = new Random();
            byte[] data = new byte[Math.Max(1024, sizeBytes)];
            random.NextBytes(data);
            File.WriteAllBytes(path, data);
        }

        private static void CreatePatternedFile(string path, int sizeBytes, byte pattern)
        {
            byte[] data = new byte[Math.Max(1024, sizeBytes)];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)((pattern + (i % 16)) % 256);
            }
            File.WriteAllBytes(path, data);
        }

        private static void CreateScriptFile(string path, int sizeBytes)
        {
            var sb = new StringBuilder();
            var random = new Random();

            string[] functionNames = { "Update", "Initialize", "Render", "ProcessInput", "LoadAssets", "PlaySound" };
            string[] varNames = { "player", "enemy", "health", "position", "velocity", "score" };

            while (sb.Length < sizeBytes)
            {
                string funcName = functionNames[random.Next(functionNames.Length)];
                string varName = varNames[random.Next(varNames.Length)];

                sb.AppendLine($"function {funcName}()");
                sb.AppendLine($"    local {varName} = {random.Next(100)}");
                sb.AppendLine($"    if {varName} > 50 then");
                sb.AppendLine($"        print('Processing {funcName}')");
                sb.AppendLine($"    end");
                sb.AppendLine($"end");
                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString().Substring(0, Math.Min(sb.Length, sizeBytes)));
        }

        private static void CreateShaderFile(string path, int sizeBytes)
        {
            var sb = new StringBuilder();
            var random = new Random();

            sb.AppendLine("#version 450");
            sb.AppendLine();
            sb.AppendLine("layout(location = 0) in vec3 position;");
            sb.AppendLine("layout(location = 1) in vec2 texCoord;");
            sb.AppendLine();
            sb.AppendLine("layout(location = 0) out vec4 fragColor;");
            sb.AppendLine();
            sb.AppendLine("void main() {");

            while (sb.Length < sizeBytes - 100)
            {
                sb.AppendLine($"    vec3 color{random.Next(100)} = vec3({random.NextDouble():F2}, {random.NextDouble():F2}, {random.NextDouble():F2});");
            }

            sb.AppendLine("    fragColor = vec4(1.0, 1.0, 1.0, 1.0);");
            sb.AppendLine("}");

            File.WriteAllText(path, sb.ToString());
        }

        private static string GenerateConfigContent(int lines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Graphics]");
            sb.AppendLine("Resolution=1920x1080");
            sb.AppendLine("FullScreen=true");
            sb.AppendLine("VSync=true");
            sb.AppendLine();
            sb.AppendLine("[Audio]");
            sb.AppendLine("MasterVolume=100");
            sb.AppendLine("MusicVolume=80");
            sb.AppendLine("SFXVolume=90");
            sb.AppendLine();

            for (int i = 0; i < lines; i++)
            {
                sb.AppendLine($"Setting{i}=Value{i}");
            }

            return sb.ToString();
        }

        private static string GenerateJsonContent(int entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"game\": {");
            sb.AppendLine("    \"title\": \"Test Game\",");
            sb.AppendLine("    \"version\": \"1.0.0\",");
            sb.AppendLine("    \"settings\": {");

            for (int i = 0; i < entries; i++)
            {
                string comma = i < entries - 1 ? "," : "";
                sb.AppendLine($"      \"option{i}\": \"value{i}\"{comma}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string GenerateReadmeContent(int words)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Test Game");
            sb.AppendLine();
            sb.AppendLine("This is a test game for compression testing.");
            sb.AppendLine();

            string[] loremWords = { "Lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit" };
            var random = new Random();

            for (int i = 0; i < words; i++)
            {
                sb.Append(loremWords[random.Next(loremWords.Length)]);
                sb.Append(" ");

                if ((i + 1) % 15 == 0)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get expected compression ratio for test data
        /// </summary>
        public static double GetExpectedCompressionRatio()
        {
            // Test data should compress to about 30-40% with Zstd-19
            // (mix of random binary, patterned data, and highly compressible text)
            return 0.35;
        }
    }
}
