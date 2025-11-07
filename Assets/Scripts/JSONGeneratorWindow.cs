#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// JSON Generator - UPDATED: Thêm support items
/// </summary>
public class JSONGeneratorWindow : EditorWindow
{
    private string fileName = "sections";
    private int numberOfSections = 10;
    private int supportItemsPerSection = 2; // NEW
    private Vector2 scrollPosition;

    [MenuItem("Tools/JSON Section Generator")]
    public static void ShowWindow()
    {
        GetWindow<JSONGeneratorWindow>("JSON Generator");
    }

    // void OnGUI()
    // {
    //     GUILayout.Label("JSON Section Generator", EditorStyles.boldLabel);
        
    //     GUILayout.Space(10);
        
    //     fileName = EditorGUILayout.TextField("File Name:", fileName);
    //     numberOfSections = EditorGUILayout.IntField("Number of Sections:", numberOfSections);
    //     supportItemsPerSection = EditorGUILayout.IntSlider("Support Items Per Section:", supportItemsPerSection, 0, 5); // NEW
        
    //     GUILayout.Space(10);
        
    //     if (GUILayout.Button("Generate Random Sections", GUILayout.Height(40)))
    //     {
    //         GenerateRandomSections();
    //     }

    //     if (GUILayout.Button("Generate Template", GUILayout.Height(40)))
    //     {
    //         GenerateTemplate();
    //     }

    //             GUILayout.Space(20);

    //     EditorGUILayout.HelpBox(
    //         "Generated files will be saved to:\n" +
    //         "Assets/Resources/SectionData/\n\n" +
    //         "Random: Tạo sections với obstacles/coins/support items ngẫu nhiên\n" +
    //         "Template: Tạo file mẫu để edit thủ công\n\n" +
    //         "Support Items sẽ được spawn theo tỉ lệ:\n" +
    //         "Ice Tea: 40%, Cold Towel: 40%, Medicine: 20%",
    //         MessageType.Info
    //     );
    // }

    void GenerateRandomSections()
    {
        SectionLibraryWrapper wrapper = new SectionLibraryWrapper
        {
            sectionLibrary = new SectionLibrary
            {
                metadata = new Metadata
                {
                    version = "1.0",
                    totalSections = numberOfSections
                },
                sections = new List<SectionData>()
            }
        };

        // Generate sections
        for (int i = 0; i < numberOfSections; i++)
        {
            int difficulty = (i / 2) + 1; // 2 sections per difficulty
            difficulty = Mathf.Clamp(difficulty, 1, 5);

            SectionData section = GenerateRandomSection(i, difficulty);
            wrapper.sectionLibrary.sections.Add(section);
        }

        // Save to JSON
        SaveJSON(wrapper);
    }

    SectionData GenerateRandomSection(int index, int difficulty)
    {
        SectionData section = new SectionData
        {
            id = $"section_{difficulty:D2}_{index:D3}",
            name = $"Random Section {index + 1}",
            length = 50f,
            difficulty = difficulty,
            obstacles = new List<ObstacleData>(),
            coins = new List<CoinGroupData>(),
            supportItems = new List<SupportItemData>() // NEW
        };

        // Generate obstacles based on difficulty
        int obstacleCount = difficulty + Random.Range(1, 3);
        
        for (int i = 0; i < obstacleCount; i++)
        {
            ObstacleData obs = new ObstacleData
            {
                type = GetRandomObstacleType(),
                lane = Random.Range(0, 3),
                zPosition = Random.Range(5f, 45f),
                yPosition = 0.5f
            };

            section.obstacles.Add(obs);
        }

        // Generate coins - CHỈ vertical_line
        int coinGroupCount = Random.Range(1, 3);
        
        for (int i = 0; i < coinGroupCount; i++)
        {
            CoinGroupData coins = new CoinGroupData
            {
                pattern = "vertical_line", // CHỈ pattern này
                lane = Random.Range(0, 3),
                zStart = Random.Range(0f, 40f),
                count = Random.Range(5, 12),
                spacing = 2.5f
            };

            section.coins.Add(coins);
        }

        // Generate support items - NEW
        for (int i = 0; i < supportItemsPerSection; i++)
        {
            SupportItemData item = new SupportItemData
            {
                lane = Random.Range(0, 3),
                zPosition = Random.Range(10f, 45f)
            };

            section.supportItems.Add(item);
        }

        return section;
    }

    string GetRandomObstacleType()
    {
        string[] types = { "barrier", "low", "high" };
        return types[Random.Range(0, types.Length)];
    }

    void GenerateTemplate()
    {
        SectionLibraryWrapper wrapper = new SectionLibraryWrapper
        {
            sectionLibrary = new SectionLibrary
            {
                metadata = new Metadata
                {
                    version = "1.0",
                    totalSections = 3
                },
                sections = new List<SectionData>()
            }
        };

        // Add template sections
        wrapper.sectionLibrary.sections.Add(CreateTemplateSection("easy", 1));
        wrapper.sectionLibrary.sections.Add(CreateTemplateSection("medium", 3));
        wrapper.sectionLibrary.sections.Add(CreateTemplateSection("hard", 5));

        SaveJSON(wrapper);
    }

    SectionData CreateTemplateSection(string prefix, int difficulty)
    {
        SectionData section = new SectionData
        {
            id = $"{prefix}_template",
            name = $"{prefix.ToUpper()} Template",
            length = 50f,
            difficulty = difficulty,
            obstacles = new List<ObstacleData>
            {
                new ObstacleData { type = "barrier", lane = 1, zPosition = 10, yPosition = 0.5f },
                new ObstacleData { type = "low", lane = 0, zPosition = 25, yPosition = 0.5f }
            },
            coins = new List<CoinGroupData>
            {
                new CoinGroupData { pattern = "vertical_line", lane = 2, zStart = 0, count = 10, spacing = 2.5f },
                new CoinGroupData { pattern = "vertical_line", lane = 1, zStart = 35, count = 5, spacing = 2.5f }
            },
            supportItems = new List<SupportItemData> // NEW
            {
                new SupportItemData { lane = 0, zPosition = 20 },
                new SupportItemData { lane = 2, zPosition = 45 }
            }
        };

        return section;
    }

    void SaveJSON(SectionLibraryWrapper wrapper)
    {
        // Create directory if not exists
        string directory = "Assets/Resources/SectionData";
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string path = $"{directory}/{fileName}.json";

        // Convert to JSON with pretty print
        string json = JsonUtility.ToJson(wrapper, true);

        // Save file
        File.WriteAllText(path, json);

        // Refresh Unity
        AssetDatabase.Refresh();

        Debug.Log($"✓ JSON file saved to: {path}");
        Debug.Log($"  Sections: {wrapper.sectionLibrary.sections.Count}");
        Debug.Log($"  Support items per section: ~{supportItemsPerSection}");
        
        EditorUtility.DisplayDialog("Success", 
            $"JSON file created!\n{path}\n\n" +
            $"Sections: {wrapper.sectionLibrary.sections.Count}\n" +
            $"Support items: {supportItemsPerSection} per section", 
            "OK");

        // Select file
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }
}

#endif