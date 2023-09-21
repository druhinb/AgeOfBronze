using RTSEngine.Cameras;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine.UI
{
    // Attached to the UI canvas that holds the hover health bar, handles displaying the hover health bar at the main camera.
    [RequireComponent(typeof(Canvas))]
    public class HoverHealthBar : PoolableObject
    {
        public IEntity Entity { private set; get; }

        private Canvas canvas;
        [SerializeField, Tooltip("Actual UI elements of the hover health bar, children of the canvas assigned above.")]
        private ProgressBarUI healthBar = new ProgressBarUI(); 

        private Transform mainCamTransform = null;

        protected HoverHealthBarUIHandler hoverHealthBarHandler { private set; get; }

        protected override void OnPoolableObjectInit()
        {
            mainCamTransform = gameMgr.GetService<IMainCameraController>().MainCamera.transform;
            this.hoverHealthBarHandler = gameMgr.GetService<HoverHealthBarUIHandler>(); 

            healthBar.Init(gameMgr);

            canvas = GetComponent<Canvas>();

            if (!logger.RequireValid(canvas,
              $"[{GetType().Name}] This component can only be attached to a game object with a '{typeof(Canvas).Name}' component attached to it!"))
                return;

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = gameMgr.GetService<IMainCameraController>().MainCamera;
        }

        void Update()
        {
            //move the canvas in order to face the camera and look at it
            transform.LookAt(transform.position + mainCamTransform.rotation * Vector3.forward,
                mainCamTransform.rotation * Vector3.up);
        }

        public void OnSpawn(HoverHealthBarSpawnInput input)
        {
            base.OnSpawn(input);

            this.Entity = input.entity;
             
            canvas.gameObject.SetActive(true);

            // Make the hover health bar canvas a child object of the source entity
            // And update its position so that it shown over the entity
            canvas.transform.SetParent(Entity.transform, true);
            canvas.transform.localPosition = new Vector3(0.0f, Entity.Health.HoverHealthBarY, 0.0f);

            healthBar.Toggle(true);

            // Initial health bar update, later updates will be triggered from the entity's health update event
            UpdateHealthBar(); 

            Entity.Health.EntityHealthUpdated += HandleEntityHealthUpdated;
            Entity.Health.EntityDead += HandleEntityDead;

        }

        private void HandleEntityDead(IEntity sender, DeadEventArgs e)
        {
            Despawn();
        }

        private void Despawn()
        {
            hoverHealthBarHandler.Despawn(this);

            Entity.Health.EntityHealthUpdated -= HandleEntityHealthUpdated;
            Entity.Health.EntityDead -= HandleEntityDead;
        }

        private void HandleEntityHealthUpdated(IEntity sender, HealthUpdateArgs e) => UpdateHealthBar();

        private void UpdateHealthBar()
        {
            if (!Entity.IsValid()) 
                return;

            healthBar.Update(Entity.Health.CurrHealth / (float)Entity.Health.MaxHealth);
        }

    }
}