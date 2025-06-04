using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // For TextMeshPro


[AddComponentMenu("Nokobot/Modern Guns/Simple Shoot")]
public class SimpleShoot : MonoBehaviour
{
    public int maxAmmo = 10; // Maximum ammo in the gun
    private int currentAmmo; // Current ammo in the gun

    [Header("Prefab Refrences")]
    public GameObject bulletPrefab;
    public GameObject casingPrefab;
    public GameObject muzzleFlashPrefab;

    [Header("Location Refrences")]
    [SerializeField] private Animator gunAnimator;
    [SerializeField] private Transform barrelLocation;
    [SerializeField] private Transform casingExitLocation;

    [Header("Audio")]
    [SerializeField] private AudioClip GunShotSound;
    [SerializeField] private AudioClip ReloadSound;

    [Header("VR Ammo Display")]
    //World Space Canvas: Creates ammo display that exists in 3D world space, not screen space
    [SerializeField] private Canvas ammoCanvas; // World Space Canvas
    [SerializeField] private TextMeshProUGUI ammoText; // Use TextMeshPro for better VR text
    [SerializeField] private Transform ammoDisplayPosition; // Where to position the ammo display
    [SerializeField] private float ammoDisplayScale = 0.01f; // Scale for VR world space
    //[SerializeField] private bool alwaysFacePlayer = true; // Should ammo always face the player
    [SerializeField] private float Ammotextsize = 5f;
    private bool isFlashing = false; // This flag prevent toggling reload and 0 by frame


    [Header("Settings")]
    [Tooltip("Specify time to destory the casing object")][SerializeField] private float destroyTimer = 2f;
    [Tooltip("Bullet Speed")][SerializeField] private float shotPower = 500f;
    [Tooltip("Casing Ejection Speed")][SerializeField] private float ejectPower = 150f;
    [Tooltip("Line width")][SerializeField] private float lineWidth = 0.5f;
    [Tooltip("Line duration")][SerializeField] private float lineDuration = 0.5f;
    [Tooltip("Line color")][SerializeField] private Color lineColor = Color.yellow;

    [Header("Raycast Settings")]
    [Tooltip("What layers can bullets hit")][SerializeField] private LayerMask bulletHitLayers = -1;
    [Tooltip("Maximum bullet range")][SerializeField] private float bulletRange = 100f;
    [Tooltip("Start offset from barrel")][SerializeField] private float barrelOffset = 0.5f;

    void Start()
    {
        if (barrelLocation == null)
            barrelLocation = transform;

        if (gunAnimator == null)
            gunAnimator = GetComponentInChildren<Animator>();

        SetupAmmoDisplay();//Setup the ammo display at start

        Reload();//Puts guns ammo to full at start
    }

    void SetupAmmoDisplay()
    {
        // If no canvas is assigned, create one
        if (ammoCanvas == null)
        {
            GameObject canvasGO = new GameObject("AmmoCanvas");
            ammoCanvas = canvasGO.AddComponent<Canvas>();
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Configure canvas for world space
        ammoCanvas.renderMode = RenderMode.WorldSpace;
        ammoCanvas.transform.SetParent(transform); // Parent to gun

        // Position the canvas
        if (ammoDisplayPosition != null)
        {
            ammoCanvas.transform.position = ammoDisplayPosition.position;
            ammoCanvas.transform.rotation = ammoDisplayPosition.rotation;
        }
        else
        {
            // Default position: slightly above and in front of gun
            ammoCanvas.transform.localPosition = new Vector3(0, 0.1f, 0.2f);
            ammoCanvas.transform.localRotation = Quaternion.identity;
        }

        // Scale the canvas for VR
        ammoCanvas.transform.localScale = Vector3.one * ammoDisplayScale;

        // Set up the text component
        if (ammoText == null)
        {
            GameObject textGO = new GameObject("AmmoText");
            textGO.transform.SetParent(ammoCanvas.transform);
            ammoText = textGO.AddComponent<TextMeshProUGUI>();
        }

        // Configure the text
        ammoText.text = currentAmmo.ToString();//Initialises, we have to update this in update
        ammoText.fontSize = Ammotextsize; // Large font size for world space
        ammoText.color = Color.white;
        ammoText.alignment = TextAlignmentOptions.Center;
        ammoText.fontStyle = FontStyles.Italic;

        // Position text in canvas
        RectTransform textRect = ammoText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Add outline to digit.
        ammoText.fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Outline");
        if (ammoText.fontSharedMaterial != null)
        {
            ammoText.outlineWidth = 0.2f;
            ammoText.outlineColor = Color.black;
        }
    }



    void Reload()
    {
        // Reset current ammo to max ammo
        SoundManager.Instance.PlaySoundOnce(ReloadSound);
        currentAmmo = maxAmmo;
        UpdateAmmoDisplay();
        //Also Add something like magazine drop like animations somethin like that.
    }


    void Update()
    {
        // Update ammo display every frame
        UpdateAmmoDisplay();

        //If you want a different input, change it here
        if (Vector3.Angle(transform.up, Vector3.up) > 100 && currentAmmo < maxAmmo)
        {
            // If the gun is tilted too much, reload automatically
            Reload();
        }
        if (Input.GetButtonDown("Fire1") && Vector3.Angle(transform.up, Vector3.up) < 100)
        {
            if (currentAmmo > 0)
            {
                gunAnimator.SetTrigger("Fire");
            }
            else
            {
                Debug.Log("Out of ammo!");
                //Task is to show this UI/Text above the gun!
            }
        }
    }


    //This function call updates the ammo display.
    void UpdateAmmoDisplay()
    {
        if (ammoText != null && !isFlashing)
        {
            ammoText.text = currentAmmo.ToString();

            // Change color based on ammo level
            if (currentAmmo == 0)
            {
                ammoText.color = Color.red;
                ShowOutOfAmmoFeedback();
            }
            else if (currentAmmo <= maxAmmo * 0.3f) // Low ammo warning
            {
                ammoText.color = Color.yellow;
            }
            else
            {
                ammoText.color = Color.white;
            }
        }
    }

    void ShowOutOfAmmoFeedback()
    {
        if (ammoText != null && !isFlashing) // Prevent multiple flashing coroutines
        {
            StartCoroutine(FlashAmmoDisplay());
        }
    }

    IEnumerator FlashAmmoDisplay()
    {
        isFlashing = true; // Set flag to prevent updates

        string originalText = ammoText.text;
        Color originalColor = ammoText.color;

        for (int i = 0; i < 3; i++)
        {
            ammoText.text = "RELOAD";
            ammoText.color = Color.red;
            yield return new WaitForSeconds(0.5f); // Increased from 0.2f

            ammoText.text = originalText;
            ammoText.color = originalColor;
            yield return new WaitForSeconds(0.5f); // Increased from 0.2f
        }

        isFlashing = false; // Reset flag
        UpdateAmmoDisplay(); // Ensure display is correct after flashing
    }

    /// <summary>
    /// Write A Player Movement Script which will disable movement through wasd and 
    /// only allow player movement through z direction.
    /// </summary>


    //This function creates the bullet behavior
    void Shoot()
    {
        //cancels if there's no bullet prefeb
        if (!bulletPrefab || currentAmmo <= 0)
            return;

        currentAmmo--; // Reduce ammo here
        Debug.Log("Ammo remaining: " + currentAmmo);


        // Create and fire the bullet
        Instantiate(bulletPrefab, barrelLocation.position, barrelLocation.rotation).GetComponent<Rigidbody>().AddForce(barrelLocation.forward * shotPower);
        SoundManager.Instance.PlaySoundOnce(GunShotSound);

        if (muzzleFlashPrefab)
        {
            //Create the muzzle flash
            GameObject tempFlash;
            tempFlash = Instantiate(muzzleFlashPrefab, barrelLocation.position, barrelLocation.rotation);

            //Destroy the muzzle flash effect
            Destroy(tempFlash, destroyTimer);
        }

        // Create tracer line effect dynamically
        CreateTracerLine();
    }

    void CreateTracerLine()
    {
        // ###Use a more advanced start position to avoid hitting the gun itself
        Vector3 rayStart = barrelLocation.position + barrelLocation.forward * barrelOffset;

        // Raycast to detect hit with layer filtering
        RaycastHit hitInfo;
        bool hasHit = Physics.Raycast(rayStart, barrelLocation.forward, out hitInfo, bulletRange, bulletHitLayers);

        // Create line dynamically
        GameObject liner = new GameObject("TracerLine");
        LineRenderer lineRenderer = liner.AddComponent<LineRenderer>();

        // ###Use Unlit shader for better glow effect
        Material tracerMaterial = new Material(Shader.Find("Unlit/Color"));
        tracerMaterial.color = lineColor;
        lineRenderer.material = tracerMaterial;

        // ###Set colors with HDR for glow
        Color glowColor = lineColor * 2f; // Brighten for glow effect
        lineRenderer.startColor = glowColor;
        lineRenderer.endColor = glowColor;

        // Configure other properties with better visibility settings
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        // ###For Cylinderical Effect
        lineRenderer.numCornerVertices = 8; // More vertices for smoother cylinder
        lineRenderer.numCapVertices = 8;    // Rounded caps
        lineRenderer.alignment = LineAlignment.View; // Always face camera

        // Ensure it renders properly
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.allowOcclusionWhenDynamic = false;
        //###
        lineRenderer.sortingOrder = 100; // Render on top

        Vector3 startPoint = rayStart;
        Vector3 endPoint = hasHit ? hitInfo.point : rayStart + barrelLocation.forward * bulletRange;

        // Add slight offset to start point to avoid clipping
        startPoint += barrelLocation.forward * 0.1f;

        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        if (hasHit)
        {
            //Debug.Log($"Hit object: {hitInfo.collider.name} on layer: {LayerMask.LayerToName(hitInfo.collider.gameObject.layer)}");
        }

        // Add glow effect using animation
        StartCoroutine(AnimateTracerGlow(lineRenderer));

        // Destroy the line after specified duration
        Destroy(liner, lineDuration);
    }

    // Coroutine to animate the tracer for better visual effect
    IEnumerator AnimateTracerGlow(LineRenderer lr)
    {
        float elapsed = 0f;
        Color originalColor = lr.startColor;

        while (elapsed < lineDuration && lr != null)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / lineDuration);
            Color newColor = originalColor;
            newColor.a = alpha;

            lr.startColor = newColor;
            lr.endColor = newColor;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    //This function creates a casing at the ejection slot
    void CasingRelease()
    {
        //Cancels function if ejection slot hasn't been set or there's no casing
        if (!casingExitLocation || !casingPrefab)
        { return; }

        //Create the casing
        GameObject tempCasing;
        tempCasing = Instantiate(casingPrefab, casingExitLocation.position, casingExitLocation.rotation) as GameObject;

        //Add force on casing to push it out
        tempCasing.GetComponent<Rigidbody>().AddExplosionForce(Random.Range(ejectPower * 0.7f, ejectPower), (casingExitLocation.position - casingExitLocation.right * 0.3f - casingExitLocation.up * 0.6f), 1f);

        //Add torque to make casing spin in random direction
        tempCasing.GetComponent<Rigidbody>().AddTorque(new Vector3(0, Random.Range(100f, 500f), Random.Range(100f, 1000f)), ForceMode.Impulse);

        //Destroy casing after X seconds
        Destroy(tempCasing, destroyTimer);
    }
}