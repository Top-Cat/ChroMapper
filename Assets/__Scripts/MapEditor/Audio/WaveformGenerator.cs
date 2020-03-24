using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class WaveformGenerator : MonoBehaviour {
    public AudioTimeSyncController atsc;
    [SerializeField] private BeatmapObjectCallbackController lookAheadController;
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private GameObject spectrogramChunkPrefab;
    [SerializeField] private Transform spectroParent;
    [SerializeField] private float saturation = 1;
    public AudioManager audioManager;
    [GradientUsage(true)]
    public Gradient spectrogramHeightGradient;

    public static float UpdateTick = 0.1f;

    private float secondPerChunk = float.NaN;
    private int chunksGenerated;

    private void Start()
    { 
        secondPerChunk = atsc.GetSecondsFromBeat(BeatmapObjectContainerCollection.ChunkSize);
        spectroParent.position = new Vector3(spectroParent.position.x, 0, -atsc.offsetBeat * EditorScaleController.EditorScale * 2);
        if (Settings.Instance.WaveformGenerator) StartCoroutine(GenerateAllWaveforms());
        else gameObject.SetActive(false);
    }

    private IEnumerator GenerateAllWaveforms()
    {
        yield return new WaitUntil(() => !SceneTransitionManager.IsLoading); //How we know "Start" has been called
        mixer.SetFloat("WaveformVolume", -80);
        while (chunksGenerated * secondPerChunk < source.clip.length)
        {
            float[][] bands = new float[audioManager.ColumnsPerChunk][];
            for (int i = 0; i < audioManager.ColumnsPerChunk; i++)
            {
                float newTime = (chunksGenerated * secondPerChunk) + (secondPerChunk / audioManager.ColumnsPerChunk * i);
                float[] samples = bands[i] = new float[AudioManager.SAMPLE_COUNT];
                source.clip.GetData(samples, Mathf.RoundToInt(newTime * source.clip.frequency));
                float[] fft = FastFourierTransform.FFT(samples);
                for (int j = 0; j < fft.Length; j++)
                {
                    bands[i][j] = fft[j];
                }
            }
            SpectrogramChunk chunk = Instantiate(spectrogramChunkPrefab, spectroParent).GetComponent<SpectrogramChunk>();
            chunk.UpdateMesh(bands, chunksGenerated, this);
            //audioManager.Start(); //WE GO AGANE
            chunksGenerated++;
            yield return new WaitForEndOfFrame();
        }
        source.time = 0;
        mixer.SetFloat("WaveformVolume", 0);
    }
}
