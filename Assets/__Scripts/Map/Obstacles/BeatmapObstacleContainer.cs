﻿using System;
using UnityEngine;

public class BeatmapObstacleContainer : BeatmapObjectContainer
{
    private static readonly int ColorTint = Shader.PropertyToID("_ColorTint");

    public override BeatmapObject objectData { get => obstacleData; set => obstacleData = (BeatmapObstacle)value; }

    [SerializeField] private TracksManager manager;

    public BeatmapObstacle obstacleData;

    public int ChunkEnd { get; private set; }

    public bool IsRotatedByNoodleExtensions => obstacleData._customData != null && (obstacleData._customData?.HasKey("_rotation") ?? false);

    public static BeatmapObstacleContainer SpawnObstacle(BeatmapObstacle data, TracksManager manager, ref GameObject prefab)
    {
        BeatmapObstacleContainer container = Instantiate(prefab).GetComponent<BeatmapObstacleContainer>();
        container.obstacleData = data;
        container.manager = manager;
        return container;
    }

    public void SetColor(Color color)
    {
        MaterialPropertyBlock.SetColor(ColorTint, color);
        UpdateMaterials();
    }

    public override void UpdateGridPosition()
    {
        float duration = obstacleData._duration;
        Vector3 localRotation = Vector3.zero;

        //Take half jump duration into account if the setting is enabled.
        if (obstacleData._duration < 0 && Settings.Instance.ShowMoreAccurateFastWalls)
        {
            float num = 60f / BeatSaberSongContainer.Instance.song.beatsPerMinute;
            float halfJumpDuration = 4;
            float songNoteJumpSpeed = BeatSaberSongContainer.Instance.difficultyData.noteJumpMovementSpeed;
            float songStartBeatOffset = BeatSaberSongContainer.Instance.difficultyData.noteJumpStartBeatOffset;

            while (songNoteJumpSpeed * num * halfJumpDuration > 18)
                halfJumpDuration /= 2;

            halfJumpDuration += songStartBeatOffset;

            if (halfJumpDuration < 1) halfJumpDuration = 1;

            duration -= duration * Mathf.Abs(duration / halfJumpDuration);
        }

        duration *= EditorScaleController.EditorScale; // Apply Editor Scale here since it can be overwritten by NE _scale Z

        if (obstacleData._customData != null)
        {
            if (obstacleData._customData.HasKey("_scale"))
            {
                if (obstacleData._customData["_scale"].Count > 2) //Apparently scale supports Z now, ok
                {
                    duration = obstacleData._customData["_scale"]?.ReadVector3().z ?? duration;
                }
            }
            if (obstacleData._customData.HasKey("_localRotation"))
            {
                localRotation = obstacleData._customData["_localRotation"]?.ReadVector3() ?? Vector3.zero;
            }
            if (obstacleData._customData.HasKey("_rotation"))
            {
                Track track = null;
                if (obstacleData._customData["_rotation"].IsNumber)
                {
                    float rotation = obstacleData._customData["_rotation"];
                    track = manager.CreateTrack(rotation);
                }
                else if (obstacleData._customData["_rotation"].IsArray)
                {
                    track = manager.CreateTrack(obstacleData._customData["_rotation"].ReadVector3());
                }
                track?.AttachContainer(this);
            }
        }

        var bounds = obstacleData.GetShape();

        // TODO: Better support GPU Batching by forcing positive scale and offsetting obstacles to match
        // Enforce positive scale, offset our obstacles to match.
        var t = transform;
        t.localPosition = new Vector3(
            bounds.Position + (bounds.Width < 0 ? bounds.Width : 0),
            bounds.StartHeight + (bounds.Height < 0 ? bounds.Height : 0),
            (obstacleData._time  + (duration < 0 ? duration : 0))* EditorScaleController.EditorScale
        );
        t.localScale = new Vector3(
            Mathf.Abs(bounds.Width),
            Mathf.Abs(bounds.Height),
            Mathf.Abs(duration)
        );
        t.localEulerAngles = localRotation;

        ChunkEnd = (int)Math.Round((objectData._time + obstacleData._duration) / (double)BeatmapObjectContainerCollection.ChunkSize,
                 MidpointRounding.AwayFromZero);
    }
}
