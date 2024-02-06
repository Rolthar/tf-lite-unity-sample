using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using ZebrarWayfinding;
using easyar;

public class FilterTwin : MonoBehaviour
{
    public Transform CameraGuess;
    public Transform TopPerformingRoot;
    public Transform ParticleRoot;

    public static FilterTwin Instance;
    public static List<SemanticItem> SemanticItems = new();

    public GameObject particlePrefab;
    public GameObject particlePrefabGen2;
    public int CullFrameTrigger = 5;

    public int particleCount = 0;
    public int topPerformingCount = 5;

    public int particleSpawnCount = 1000;
    public int particleSecondGenSpawnCount = 50;
    public float semanticEdge = 0.5f;

    private List<ParticleScript> particles = new();

    public List<ParticleScript> topPerformingParticles = new();

    public List<MeshRenderer> levels = new();
    public List<Bounds> levelsBounds = new();

    public List<Area> Areas = new();
    public List<Area> PotentialUserAreas = new();

    public TMP_Text title;
    public TMP_Text semanticEstimateOptions;
    public TMP_Text semanticsCameraText;
    public TMP_Text cameraHeight;

    public MeshRenderer mesh;

    public bool cullParticles = false;
    private int frameCount = 0;

    public float compareThreshold = 0.5f;
    public float probabilityThreshold = 1f;

    public float smallestDiff = 1f;

    private List<SemanticItemType> cameraItemsCopy = new();

    public float? CameraFloorOffset = 0;

    public DenseSpatialMapBuilderFrameFilter DenseMapBuilder;

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
            if (frameCount >= CullFrameTrigger)
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
        UpdateCameraVerticalOffset();

    }

    public void UpdateCameraVerticalOffset()
    {
        Ray ray = new Ray(CameraScript.Instance.transform.position, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 3f, CameraScript.Instance.raycastScript.hitLayers))
        {
            CameraFloorOffset = hit.distance;
            cameraHeight.text = $"Offset:{CameraFloorOffset.Value.ToString()}";
            Debug.DrawLine(CameraScript.Instance.transform.position, hit.point, Color.green);

        }
        else
        {
            Debug.DrawRay(CameraScript.Instance.transform.position, Vector3.down * 3f, Color.red);
        }

    }

    public void UpdatePotentialAreas(List<SemanticItem> cameraItems)
    {
        string debugStringDemantics = "Editor Semantics: {";
        cameraItemsCopy = cameraItems.Select(x => x.type).ToList();

        if (cameraItems.Count > 0)
        {
            foreach (SemanticItem _item in cameraItems)
            {
                debugStringDemantics += _item.type.ToString();
            }
            var potentialAreas = GetMatchingAreas(Areas, cameraItems);
            PotentialUserAreas = potentialAreas;
            string debugString = "Potential Areas from semantics: {";
            foreach (Area _area in potentialAreas)
            {
                debugString += _area.Name;
            }
            debugString += "}";
            semanticEstimateOptions.text = debugString;
        }
        debugStringDemantics += "}";
        semanticsCameraText.text = debugStringDemantics;
    }

    public void UpdatePotentialAreas(List<SemanticItemType> cameraItems)
    {
        string debugStringDemantics = "Device Semantics: {";
        cameraItemsCopy = cameraItems;

        if (cameraItems.Count > 0)
        {
            foreach (SemanticItemType _item in cameraItems)
            {
                debugStringDemantics += _item.ToString();
            }
            var potentialAreas = GetMatchingAreas(Areas, cameraItems);
            PotentialUserAreas = potentialAreas;
            string debugString = "Potential Areas from semantics: {";
            foreach (Area _area in potentialAreas)
            {
                debugString += _area.Name;
            }
            debugString += "}";
            semanticEstimateOptions.text = debugString;
        }
        debugStringDemantics += "}";
        semanticsCameraText.text = debugStringDemantics;
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
                matchingAreas.Add(area);
        }
        return matchingAreas;
    }

    public List<Area> GetMatchingAreas(List<Area> areas, List<SemanticItemType> cameraSemanticItemTypes)
    {
        // Count the occurrences of each SemanticItemType seen by the camera
        var cameraItemTypeCounts = cameraSemanticItemTypes.GroupBy(type => type)
            .ToDictionary(group => group.Key, group => group.Count());

        var matchingAreas = new List<Area>();

        foreach (var area in areas)
        {
            var areaItemTypeCounts = area.SemanticsInArea.GroupBy(item => item.type)
                .ToDictionary(group => group.Key, group => group.Count());

            // Check if the current area matches the camera's view
            bool isMatch = cameraItemTypeCounts.All(cameraItemType =>
                areaItemTypeCounts.TryGetValue(cameraItemType.Key, out var countInArea) && countInArea >= cameraItemType.Value);

            if (isMatch)
                matchingAreas.Add(area);
        }
        return matchingAreas;
    }

    public void CompareParticles()
    {
        List<ParticleScript> particlesToRemove = new List<ParticleScript>();

        if (cameraItemsCopy.Count > 0)
        {
            foreach (var particle in particles)
            {
                List<SemanticItemType> conecastItemTypes = particle.conecast.SemanticGazeList.Select(item => item.type).ToList();
                // Lists to hold matching and non-matching items
                List<SemanticItemType> matchingItems = new List<SemanticItemType>();
                List<SemanticItemType> nonMatchingItems = new List<SemanticItemType>();

                // Count occurrences of each type in both lists
                var cameraItemCount = cameraItemsCopy.GroupBy(x => x)
                    .ToDictionary(x => x.Key, x => x.Count());

                var conecastItemCount = conecastItemTypes.GroupBy(x => x)
                    .ToDictionary(x => x.Key, x => x.Count());

                bool allmatch = true;
                foreach (var item in particle.conecast.SemanticGazeList)
                {
                    // Determine if the current item's type matches the occurrences in the camera's list
                    bool isMatch = cameraItemCount.TryGetValue(item.type, out int cameraCount) &&
                                   conecastItemCount.TryGetValue(item.type, out int conecastCount) &&
                                   cameraCount == conecastCount;

                    if (isMatch)
                    {
                        matchingItems.Add(item.type);
                    }
                    else
                    {
                        nonMatchingItems.Add(item.type);
                        allmatch = false;
                    }
                }
                particle.matchingSemanticsCount = matchingItems.Count;
                Debug.Log($" uuid: {particle.uuid}, {conecastItemCount.Count / cameraItemCount.Count}, cone: {conecastItemCount.Count}, cameraItemCount:{cameraItemCount.Count} ");

                if (allmatch)
                    particle.totalDifference *= semanticEdge;

            }
        }

        var particlesWithoutSemantics = new List<ParticleScript>(particles.Where(_particles => _particles.matchingSemanticsCount == 0).ToList());
        var particlesWithSemantics = new List<ParticleScript>(particles.Where(_particles => _particles.matchingSemanticsCount > 0).ToList());
        particlesWithSemantics = particlesWithSemantics.OrderBy(p => p.matchingSemanticsCount).ToList();
        foreach (var particle in particlesWithoutSemantics)
        {
            if (!particle.ProvedItself)
            {
                particles.Remove(particle);
                Destroy(particle.transform.parent.gameObject);
                particleCount--;
            }
        }

        if (particles.Count > 0)
        {
            particles = new List<ParticleScript>(particlesWithSemantics).OrderBy(p => p.totalDifference * p.matchingSemanticsCount).ToList();
            smallestDiff = particles.OrderBy(p => p.totalDifference).FirstOrDefault().totalDifference;

            title.text = GetAreaAndLevelName(particles[0]);
            Debug.Log($"Top: {particles[0].uuid}, {particles[0].totalDifference}");
            CameraGuess.transform.position = particles[0].transform.position;
            CameraGuess.transform.rotation = particles[0].transform.rotation;

            topPerformingParticles = new List<ParticleScript>(particles.Take(topPerformingCount).ToList());

            foreach (var particle in topPerformingParticles)
            {
                particle.transform.parent.parent = TopPerformingRoot;
            }
            Debug.Log(topPerformingParticles.Count);
            particlesToRemove = new List<ParticleScript>(particles.Skip(topPerformingCount).ToList());
            foreach (var particle in particlesToRemove)
            {
                if (!particle.ProvedItself)
                {
                    particles.Remove(particle);
                    Destroy(particle.transform.parent.gameObject);
                    particleCount--;
                }
            }

            SpawnNewParticles(topPerformingParticles, smallestDiff);
        }

        SpawnParticles();
    }


    public void SpawnNewParticles(List<ParticleScript> bestPerformingParticles, float maxSpawnRadius)
    {
        List<ParticleScript> newParticles = new List<ParticleScript>();

        foreach (var particle in bestPerformingParticles)
        {
            float age = particle != null ? particle.Age : 0f;
            float spawnRadius = CalculateSpawnRadius(age, maxSpawnRadius);
            var semanticsInArea = particle.nearestArea.SemanticsInArea.Where(semantic => cameraItemsCopy.Contains(semantic.type)).ToList();

            for (int j = 0; j < particleSecondGenSpawnCount; j++)
            {

                Vector3 spawnPosition = particle.transform.position + Random.insideUnitSphere * spawnRadius;
                spawnPosition.y = CameraFloorOffset.HasValue ? (particle.nearestArea.level.Floor.position.y + CameraFloorOffset.Value) : spawnPosition.y;
                spawnPosition = EnsurePositionWithinBounds(spawnPosition);

                Quaternion randomRotation = Quaternion.Euler(Camera.main.transform.eulerAngles.x, particle.transform.eulerAngles.y + Random.Range(-15f, 15f), Camera.main.transform.eulerAngles.z);
                if (semanticsInArea.Count > 0)
                {
                    var lookPosition = semanticsInArea[Random.Range(0, semanticsInArea.Count)].transform.position;
                    Vector3 directionToLook = (lookPosition - particle.transform.position).normalized;
                    // Create a rotation that looks at lookPosition
                    Quaternion lookRotation = Quaternion.LookRotation(directionToLook);

                    // Extract the Y component of this rotation
                    float yRotation = lookRotation.eulerAngles.y + Random.Range(-15f, 15f);

                    // Combine Camera's X and Z with the Y component from lookRotation
                    randomRotation = Quaternion.Euler(Camera.main.transform.eulerAngles.x, yRotation, Camera.main.transform.eulerAngles.z);
                }

                var newParticle = Instantiate(particlePrefabGen2, ParticleRoot);
                particleCount++;
                var script = newParticle.GetComponentInChildren<ParticleScript>();
                script.isFirstGen = false;
                script.transform.localPosition = spawnPosition;
                script.transform.localRotation = randomRotation;
                script.nearestArea = particle.nearestArea;
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
            radius = Mathf.Lerp(maxRadius, minRadius, age / 3f);
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
                var semanticsInArea = area.SemanticsInArea.Where(semantic => cameraItemsCopy.Contains(semantic.type)).ToList();
                var spawnBounds = area.rend.bounds;
                var semanticsBounds = CreateBoundsForEachGameObject(semanticsInArea.Select(x => x.gameObject).ToList(), 5);
                spawnPerLevel /= semanticsBounds.Count > 0 ? semanticsBounds.Count : 1;


                foreach (Bounds _bounds in semanticsBounds)
                {
                    for (int i = 0; i < spawnPerLevel; i++)
                    {

                        Bounds intersectionBounds = new Bounds();
                        if (spawnBounds.Intersects(_bounds))
                        {
                            intersectionBounds.SetMinMax(
                                Vector3.Max(spawnBounds.min, _bounds.min),
                                Vector3.Min(spawnBounds.max, _bounds.max)
                            );
                            SpawnObjectWithinBounds(intersectionBounds, area, _bounds.center);
                        }
                        else
                            SpawnObjectWithinBounds(_bounds, area, _bounds.center);
                    }
                }



            }
        }
        else
        {
            int spawnPerLevel = particleSpawnCount / Areas.Count;
            foreach (var area in Areas)
            {
                for (int i = 0; i < spawnPerLevel; i++)
                {
                    SpawnObjectWithinBounds(area.rend.bounds, area);
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

    public List<Bounds> CreateBoundsForEachGameObject(List<GameObject> gameObjects, float radius)
    {
        List<Bounds> boundsList = new List<Bounds>();

        foreach (var gameObject in gameObjects)
        {
            if (gameObject != null)
            {
                // Create bounds for each GameObject using its position and the specified radius (_x)
                Bounds bounds = new Bounds(gameObject.transform.position, Vector3.one * (radius * 2)); // Radius * 2 to get the diameter for bounds size
                boundsList.Add(bounds);
            }
        }

        return boundsList;
    }

    void SpawnObjectWithinBounds(Bounds bounds, Area area, Vector3? lookPosition = null)
    {

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = CameraFloorOffset.HasValue ? (area.level.Floor.position.y + CameraFloorOffset.Value) : Random.Range(bounds.min.y, bounds.max.y);
        float z = Random.Range(bounds.min.z, bounds.max.z);

        Vector3 randomPosition = new Vector3(x, y, z);
        Quaternion randomRotation = Quaternion.Euler(Camera.main.transform.eulerAngles.x, Random.Range(0, 360), Camera.main.transform.eulerAngles.z);

        if (lookPosition.HasValue)
        {
            Vector3 directionToLook = (lookPosition.Value - randomPosition).normalized;
            // Create a rotation that looks at lookPosition
            Quaternion lookRotation = Quaternion.LookRotation(directionToLook);

            // Extract the Y component of this rotation
            float yRotation = lookRotation.eulerAngles.y + Random.Range(-15f, 15f);

            // Combine Camera's X and Z with the Y component from lookRotation
            randomRotation = Quaternion.Euler(Camera.main.transform.eulerAngles.x, yRotation, Camera.main.transform.eulerAngles.z);
        }

        var particle = Instantiate(particlePrefab, ParticleRoot);
        particleCount++;
        var script = particle.GetComponentInChildren<ParticleScript>();
        script.transform.localPosition = randomPosition;
        script.transform.localRotation = randomRotation;
        script.nearestArea = area;
        particles.Add(script);

    }


    public string GetAreaAndLevelName(ParticleScript obj)
    {
        foreach (Area area in Areas)
        {
            if (area.rend.bounds.Contains(obj.transform.position))
            {
                // Format the string to return Area Name and Level Name
                return area.Name + " " + (area.level != null ? area.level.Name : "No Level") + " (" + obj.totalDifference + ")";
            }
        }
        return "Not in any Area";
    }
}
