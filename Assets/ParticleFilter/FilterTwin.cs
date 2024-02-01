using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using ZebrarWayfinding;

public class FilterTwin : MonoBehaviour
{
    public static FilterTwin Instance;
    public static List<SemanticItem> SemanticItems = new();

    public GameObject particlePrefab;
    public GameObject particlePrefabGen2;


    public int particleSpawnCount = 1000;
    public int particleSecondGenSpawnCount = 50;

    private List<ParticleScript> particles = new();
    public List<MeshRenderer> levels = new();
    public List<Bounds> levelsBounds = new();

    public List<Area> Areas = new();
    public List<Area> PotentialUserAreas = new();

    public TMP_Text title;
    public MeshRenderer mesh;

    public bool cullParticles = false;
    private int frameCount = 0;

    public float compareThreshold = 0.5f;
    public float probabilityThreshold = 1f;

    public float smallestDiff = 1f;

    void Awake()
    {
        Instance = this;
        SemanticItems = new();
    }

    void Start()
    {

        foreach (var meshRenderer in levels)
        {
            if (meshRenderer != null)
            {
                levelsBounds.Add(meshRenderer.bounds);
            }
        }

        SpawnParticles();
    }

    void Update()
    {
        if (cullParticles)
        {
            if (frameCount >= 3)
            {
                CompareParticles();
                frameCount = 0;
            }
            frameCount++;
        }
        if (probabilityThreshold - Time.deltaTime > 0.3)
            probabilityThreshold -= Time.deltaTime;

        // foreach (SemanticItem item in SemanticItems)
        //     Debug.Log(item.name);

    }

    public void UpdatePotentialAreas(List<SemanticItem> cameraItems)
    {
        if (cameraItems.Count > 0)
        {
            var potentialAreas = GetMatchingAreas(Areas, cameraItems);
            PotentialUserAreas = potentialAreas;
            string debugString = "Potential Areas from semantics: {";
            foreach (Area _area in potentialAreas)
            {
                debugString += _area.Name;
            }
            debugString += "}";
            Debug.Log(debugString);
        }
    }


    public List<Area> GetMatchingAreas(List<Area> areas, List<SemanticItem> cameraSemanticItems)
    {
        // Count the occurrences of each SemanticItemType seen by the camera
        var cameraItemCounts = cameraSemanticItems.GroupBy(item => item.type)
            .ToDictionary(group => group.Key, group => group.Count());

        var matchingAreas = new List<Area>();

        foreach (var area in areas)
        {
            var areaItemCounts = area.SemanticsInArea.GroupBy(item => item.type)
                .ToDictionary(group => group.Key, group => group.Count());

            // This flag indicates if the current area matches the camera's view
            bool isMatch = cameraItemCounts.All(cameraItem =>
                areaItemCounts.TryGetValue(cameraItem.Key, out var countInArea) && countInArea >= cameraItem.Value);

            if (isMatch)
            {
                matchingAreas.Add(area);
            }
        }

        return matchingAreas;
    }

    // public static List<Area> GetMatchingAreas(List<Area> areas, List<SemanticItem> cameraSemanticItems)
    // {
    //     // Extract the distinct types of SemanticItems seen by the camera
    //     var cameraItemTypes = new HashSet<SemanticItemType>(cameraSemanticItems.Select(item => item.type));

    //     var matchingAreas = new List<Area>();

    //     foreach (var area in areas)
    //     {
    //         // Create a set of item types present in the area
    //         var areaItemTypes = new HashSet<SemanticItemType>(area.SemanticsInArea.Select(item => item.type));

    //         // Check if all item types seen by the camera are present in the area
    //         if (!cameraItemTypes.Except(areaItemTypes).Any())
    //         {
    //             matchingAreas.Add(area);
    //         }
    //     }

    //     return matchingAreas;
    // }


    public static List<Area> GetAreasContainingAllCameraItems(List<Area> areas, List<SemanticItem> cameraSemanticItems)
    {
        // Convert the list of SemanticItems seen by the camera into a list of their types
        var cameraItemTypes = new HashSet<SemanticItemType>(cameraSemanticItems.Select(item => item.type));

        // Find areas that contain all the semantic item types seen by the camera
        var matchingAreas = new List<Area>();

        foreach (var area in areas)
        {
            // Get the types of SemanticItems in the current area
            var areaItemTypes = new HashSet<SemanticItemType>(area.SemanticsInArea.Select(item => item.type));

            // Check if this area contains all types of items seen by the camera
            if (cameraItemTypes.All(type => areaItemTypes.Contains(type)))
            {
                matchingAreas.Add(area);
            }
        }

        return matchingAreas;
    }


    public void CompareParticles()
    {
        List<ParticleScript> particlesToRemove = new List<ParticleScript>();
        particles = particles.OrderBy(p => p.totalDifference).ToList();
        smallestDiff = particles.OrderBy(p => p.totalDifference).FirstOrDefault().totalDifference;

        for (int i = 5; i < particles.Count; i++)
        {
            particlesToRemove.Add(particles[i]);
        }


        // Remove the particles after the iteration
        var removeCount = particlesToRemove.Count;
        foreach (var particle in particlesToRemove)
        {
            particles.Remove(particle);
            Destroy(particle.transform.parent.gameObject);
        }
        title.text = GetAreaAndLevelName(particles[0].gameObject);

        var best5 = new List<ParticleScript>(particles.Take(5).ToList());
        particlesToRemove = new List<ParticleScript>(particles.Skip(5).ToList());
        foreach (var particle in particlesToRemove)
        {
            particles.Remove(particle);
            Destroy(particle.transform.parent.gameObject);
        }
        SpawnParticles();
        SpawnNewParticles(best5, smallestDiff);


    }
    public void SpawnNewParticles(int count, float maxSpawnRadius)
    {
        List<ParticleScript> newParticles = new List<ParticleScript>();

        foreach (var particle in particles)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject part = particle.gameObject;

                float age = particle != null ? particle.Age : 0f;
                float spawnRadius = CalculateSpawnRadius(age, maxSpawnRadius);


                Vector3 spawnPosition = part.transform.position + Random.insideUnitSphere * spawnRadius;
                spawnPosition = EnsurePositionWithinBounds(spawnPosition);

                // Quaternion randomRotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));

                var newParticle = Instantiate(particlePrefab);
                var script = newParticle.GetComponentInChildren<ParticleScript>();
                script.transform.localPosition = spawnPosition;
                script.transform.localRotation = Camera.main.transform.rotation;
                newParticles.Add(script);
            }
        }

        // Merge new particles with the existing list
        particles.AddRange(newParticles);
    }

    public void SpawnNewParticles(List<ParticleScript> bestPerformingParticles, float maxSpawnRadius)
    {
        List<ParticleScript> newParticles = new List<ParticleScript>();

        foreach (var particle in bestPerformingParticles)
        {
            float age = particle != null ? particle.Age : 0f;
            float spawnRadius = CalculateSpawnRadius(age, maxSpawnRadius);

            for (int j = 0; j < particleSecondGenSpawnCount; j++)
            {

                Vector3 spawnPosition = particle.transform.position + Random.insideUnitSphere * spawnRadius;
                spawnPosition = EnsurePositionWithinBounds(spawnPosition);

                Quaternion randomRotation = Quaternion.Euler(Camera.main.transform.eulerAngles.x, Random.Range(0, 360), Camera.main.transform.eulerAngles.z);

                var newParticle = Instantiate(particlePrefabGen2);
                var script = newParticle.GetComponentInChildren<ParticleScript>();
                script.isFirstGen = false;
                script.transform.localPosition = spawnPosition;
                script.transform.localRotation = randomRotation;
                newParticles.Add(script);
            }

        }

        // Merge new particles with the existing list
        particles.AddRange(newParticles);
    }

    float CalculateSpawnRadius(float age, float maxRadius)
    {
        // Linearly decrease the radius to 0.01 over 5 seconds
        float minRadius = 0.01f;
        float radius;

        if (age < 5f)
        {
            // Interpolate between maxRadius and minRadius based on age
            radius = Mathf.Lerp(maxRadius, minRadius, age / 5f);
        }
        else
        {
            radius = minRadius;
        }

        return radius;
    }


    [ContextMenu("SpawnParticles")]
    public void SpawnParticles()
    {
        if (PotentialUserAreas.Count > 0)
        {
            int spawnPerLevel = particleSpawnCount / PotentialUserAreas.Count;
            foreach (var area in PotentialUserAreas)
            {
                for (int i = 0; i < spawnPerLevel; i++) // Example: spawn 10 objects
                {
                    SpawnObjectWithinBounds(area.rend.bounds);
                }
            }
        }
        else
        {
            int spawnPerLevel = particleSpawnCount / levels.Count;
            foreach (var level in levels)
            {
                for (int i = 0; i < spawnPerLevel; i++) // Example: spawn 10 objects
                {
                    SpawnObjectWithinBounds(level.bounds);
                }
            }
        }


    }

    private Vector3 EnsurePositionWithinBounds(Vector3 position)
    {
        foreach (var bound in levelsBounds)
        {
            if (bound.Contains(position))
            {
                return position; // Position is already within a bound
            }
        }

        // If here, position is not within any bounds
        // Find the nearest bound and nearest point within that bound
        Bounds nearestBound = levelsBounds[0];
        float minDistance = Mathf.Infinity;

        foreach (var bound in levelsBounds)
        {
            float distance = (bound.ClosestPoint(position) - position).sqrMagnitude;
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestBound = bound;
            }
        }

        // Return the nearest point within the nearest bound
        return nearestBound.ClosestPoint(position);
    }

    void SpawnObjectWithinBounds(Bounds bounds)
    {

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        float z = Random.Range(bounds.min.z, bounds.max.z);

        Vector3 randomPosition = new Vector3(x, y, z);
        Quaternion randomRotation = Quaternion.Euler(Camera.main.transform.eulerAngles.x, Random.Range(0, 360), Camera.main.transform.eulerAngles.z);

        var particle = Instantiate(particlePrefab);
        var script = particle.GetComponentInChildren<ParticleScript>();
        script.transform.localPosition = randomPosition;
        script.transform.localRotation = randomRotation;
        particles.Add(script);

    }


    public string GetAreaAndLevelName(GameObject obj)
    {
        foreach (Area area in Areas)
        {
            if (area.rend.bounds.Contains(obj.transform.position))
            {
                // Format the string to return Area Name and Level Name
                return area.Name + " " + (area.level != null ? area.level.Name : "No Level");
            }
        }
        return "Not in any Area";
    }
}
