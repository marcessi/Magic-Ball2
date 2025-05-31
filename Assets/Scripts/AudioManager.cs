using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance { get { return _instance; } }
    
    // Pool de AudioSources para diferentes canales de sonido
    private Dictionary<string, AudioSource> audioChannels = new Dictionary<string, AudioSource>();
    
    // For tracking recently played sounds - simplificado a un solo intervalo
    private Dictionary<string, float> lastPlayedTimes = new Dictionary<string, float>();
    private const float SOUND_INTERVAL = 0.1f; // Intervalo estándar para todos los sonidos
    
    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Inicializar canales de audio predeterminados
        CreateAudioChannel("default");
        CreateAudioChannel("hit-bloque");
        CreateAudioChannel("rebote");
        CreateAudioChannel("hit-pared");
        CreateAudioChannel("powerUp");
    }
    
    // Crea un nuevo canal de audio (AudioSource)
    private AudioSource CreateAudioChannel(string channelName)
    {
        // Crear un objeto hijo para el canal
        GameObject channelObj = new GameObject("AudioChannel_" + channelName);
        channelObj.transform.SetParent(transform);
        
        // Añadir AudioSource al objeto
        AudioSource source = channelObj.AddComponent<AudioSource>();
        
        // Configuración por defecto
        source.spatialBlend = 0f; // Audio 2D (mono)
        source.panStereo = 0f;    // Centrado
        source.playOnAwake = false;
        
        // Guardar en el diccionario
        audioChannels[channelName] = source;
        return source;
    }

    public void PlaySound(AudioClip clip, Vector3 position, float volume = 1.0f)
    {
        if (clip == null) return;
        
        string clipName = clip.name;
        float currentTime = Time.time;
        
        // Comprobación simplificada: solo un intervalo para todos los sonidos
        if (lastPlayedTimes.ContainsKey(clipName))
        {
            if (currentTime - lastPlayedTimes[clipName] < SOUND_INTERVAL)
            {
                return; // Omitir este sonido
            }
        }
        
        // Actualizar tiempo de la última reproducción
        lastPlayedTimes[clipName] = currentTime;
        
        // Determinar qué canal usar según el tipo de sonido
        string channelName = "default";
        
        // Mapear clips a canales específicos
        if (clipName.Contains("bloque"))
            channelName = "hit-bloque";
        else if (clipName.Contains("rebote") || clipName.Contains("paddle"))
            channelName = "rebote";
        else if (clipName.Contains("pared") || clipName.Contains("wall"))
            channelName = "hit-pared";
        else if (clipName.Contains("power"))
            channelName = "powerUp";
        
        // Obtener o crear el canal
        AudioSource channel;
        if (!audioChannels.TryGetValue(channelName, out channel))
        {
            channel = CreateAudioChannel(channelName);
        }
        
        // Configurar el AudioSource para reproducir en mono
        channel.clip = clip;
        channel.volume = volume;
        channel.pitch = 1f;
        
        // Reproducir el sonido inmediatamente
        channel.Play();
    }
}