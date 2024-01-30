using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class FilterTwin : MonoBehaviour
{
    public static FilterTwin Instance;

    public GameObject particlePrefab;
    public GameObject particlePrefabGen2;


    public int particleSpawnCount = 1000;
    public int particleSecondGenSpawnCount = 50;

    private List<ParticleScript> particles = new();
    public List<MeshRenderer> levels = new();
    public List<Bounds> levelsBounds = new();

    public List<Area> Areas = new();

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
        // foreach (var particle in particles)
        // {
        //     ParticleScript particleScript = particle.GetComponentInChildren<ParticleScript>();

        //     if (particleScript != null)
        //     {
        //         if (particleScript.totalDifference > probabilityThreshold)
        //             particlesToRemove.Add(particle);

        //         // bool shouldRemove = false;
        //         // foreach (var cameraHit in CameraScript.Instance.lastHits)
        //         // {
        //         //     string key = cameraHit.Key;
        //         //     float? cameraHitValue = cameraHit.Value;
        //         //     float? particleHitValue = particleScript.lastHits.ContainsKey(key) ? particleScript.lastHits[key] : null;

        //         //     if (particleHitValue.HasValue && cameraHitValue.HasValue &&
        //         //         Mathf.Abs(particleHitValue.Value - cameraHitValue.Value) > compareThreshold)
        //         //     {
        //         //         shouldRemove = true;
        //         //         break;
        //         //     }
        //         // }

        //         // if (shouldRemove)
        //         // {
        //         //     particlesToRemove.Add(particle);
        //         // }
        //     }
        // }

        // Remove the particles after the iteration
        var removeCount = particlesToRemove.Count;
        foreach (var particle in particlesToRemove)
        {
            particles.Remove(particle);
            Destroy(particle.transform.parent.gameObject);
        }

        // if (particles.Count == 0 || smallestDiff > 5)
        // {
        //     particlesToRemove = new List<ParticleScript>(particles);
        //     foreach (var particle in particlesToRemove)
        //     {
        //         particles.Remove(particle);
        //         Destroy(particle.transform.parent.gameObject);
        //     }
        //     SpawnParticles();
        // }
        // else if (particles.Count <= particleSpawnCount)
        // {     // Example usage in CompareParticles, after culling

        //     SpawnNewParticles(Mathf.RoundToInt((float)particleSpawnCount / (float)particles.Count), smallestDiff);
        // }

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

    void InstantiateAndAddParticle(Vector3 position)
    {
        // Quaternion randomRotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        var particle = Instantiate(particlePrefab);
        var script = particle.GetComponentInChildren<ParticleScript>();
        script.transform.localPosition = position;
        script.transform.localRotation = Camera.main.transform.rotation;
        particles.Add(script);
    }

    [ContextMenu("SpawnParticles")]
    public void SpawnParticles()
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
