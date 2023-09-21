using UnityEngine;

[ExecuteAlways]
public class LightingManager : MonoBehaviour
{
    //Scene References
    [SerializeField] private Light DirectionalLight;
    [SerializeField] private Light MoonLight;
    [SerializeField] private LightingPreset Preset;
    [SerializeField] private float DayScalingConstant;
    //Variables
    [SerializeField, Range(0, 24)] private float TimeOfDay;


    private void Update()
    {
        if (Preset == null)
            return;

        if (Application.isPlaying)
        {
            //(Replace with a reference to the game time)
            TimeOfDay += Time.deltaTime / DayScalingConstant;
            TimeOfDay %= 24; //Modulus to ensure always between 0-24

            // //Turning off light when not necessary.
            // MoonLight.enabled = TimeOfDay > 18 || TimeOfDay  < 6;
            // DirectionalLight.enabled = TimeOfDay <= 18 && TimeOfDay  >= 6;
            UpdateLighting(TimeOfDay / 24f);
        }
        else
        {
            //Turning off light when not necessary.
            MoonLight.enabled = TimeOfDay > 18 || TimeOfDay  < 6;
            DirectionalLight.enabled = TimeOfDay <= 18 && TimeOfDay  >= 6;
            UpdateLighting(TimeOfDay / 24f);
        }
    }


    private void UpdateLighting(float timePercent)
    {
        //Set ambient and fog
        RenderSettings.ambientLight = Preset.AmbientColor.Evaluate(timePercent);
        // RenderSettings.fogColor = Preset.FogColor.Evaluate(timePercent);

        DirectionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, 90f, 0));
        MoonLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) + 90f, 90f, 0));

    }



    //Try to find a directional light to use if we haven't set one
    private void OnValidate()
    {
        if (DirectionalLight != null)
            return;

        //Search for lighting tab sun
        if (RenderSettings.sun != null)
        {
            DirectionalLight = RenderSettings.sun;
        }
        //Search scene for light that fits criteria (directional)
        else
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    DirectionalLight = light;
                    return;
                }
            }
        }
    }

}
