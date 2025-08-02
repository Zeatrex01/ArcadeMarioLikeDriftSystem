﻿using UnityEngine;

public class PlayerInputProvider : MonoBehaviour
{

    [SerializeField]
    private KartController kart = null;

    private void Update()
    {
        if (kart == null)
        {
            return;
        }

        // ZAS: If we are accelerating, tell the kart to accelerate
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            kart.Accelerate();

        // ZAS: Tell the kart how to steer each update
        float horizontalMovement = Input.GetAxis("Horizontal");
        kart.Steer(horizontalMovement);

        // ZAS: Jump/Drift control
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Joystick1Button0))
            kart.Jump();
    }

}
