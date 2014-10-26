﻿using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public float secondsToRotate = 1f;
    public KeyCode rotateRightKey = KeyCode.E;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode resetKey = KeyCode.R;
    
	BlockManager blockManager;
	NoRotationManager noRotationManager;

	public Int2 currentRotationCenter;
    int currentRotationDirection = 0;
    float rotationClock = 0f;
    public enum RotationMode { playing, frozen, rotating };
    public RotationMode gameState = RotationMode.playing;
    PlayerMovement playerMovement;

    public bool gameFrozen
    {
        get
        {
            if (gameState == RotationMode.playing)
                return false;
            else
                return true;
        }
    }

    void Awake()
    {
        this.blockManager = FindObjectOfType<BlockManager>();
        this.playerMovement = FindObjectOfType<PlayerMovement>();
		this.noRotationManager = FindObjectOfType<NoRotationManager>();
    }

    public void RegisterClick(float clickx, float clicky)
    {
    }

    void Update()
    {
        if (Input.GetKey(resetKey))
        {
            ResetLevel();
        }
        switch (gameState)
        {
            case RotationMode.playing:
                {
					if(Input.GetMouseButtonDown(0))
					{
						Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
						int x = Mathf.RoundToInt(worldPos.x);
						int y = Mathf.RoundToInt(worldPos.y);
                        this.currentRotationCenter = new Int2(x, y);
						if(isValidCenter(currentRotationCenter) && playerMovement.isGrounded() && !playerMovement.beingShot && !playerInNoRoZone()){
							gameState = RotationMode.frozen;
						}
						
					}
                    break;
                }

            case RotationMode.frozen: //game is frozen, left-click is held, but no rotation is happening
                {
					if(!Input.GetMouseButton(0))
					{
                        if (this.rotationClock <= 0f)
						    gameState = RotationMode.playing;
					}
                    // If we're not already rotating
                    if (rotationClock <= 0f)
                    {
                        // Rotate right!
                        if (Input.GetKey(rotateRightKey) && blockManager.isValidRotation(currentRotationCenter, -1))
                        {
                            blockManager.startRotation(currentRotationCenter);
                            rotationClock = 1f;
                            currentRotationDirection = -1;
                            gameState = RotationMode.rotating;
                        }
                        // Rotate left!
                        else if (Input.GetKey(rotateLeftKey) && blockManager.isValidRotation(currentRotationCenter, 1))
                        {
                            blockManager.startRotation(currentRotationCenter);
                            rotationClock = 1f;
                            currentRotationDirection = 1;
                            gameState = RotationMode.rotating;
                        }
                    }

                    break;
                }

            case RotationMode.rotating:
                {
					//check rotation clock, if it's done set gameState back to RotationMode.frozen
                    if (rotationClock > 0)
                        rotationClock -= Time.deltaTime / secondsToRotate;
                    // If we're done rotating, finish the rotation
                    if (rotationClock <= 0)
                    {
                        blockManager.finishRotation(currentRotationCenter, currentRotationDirection);
                        gameState = RotationMode.frozen;
                    }
                    else
                    {
                        blockManager.AnimateFrameOfRotation(currentRotationCenter, currentRotationDirection, 1f - rotationClock);
                    }
                    break;
                }
        }
    }

	public bool isValidCenter(Int2 xy){

		if (noRotationManager.hasNoRotationZone(xy)) {
			return false;
		}

		Int2 playerPos = blockManager.player.GetRoundedPosition();
		int absDx = Mathf.Abs (xy.x - playerPos.x);
		int absDy = Mathf.Abs (xy.y - playerPos.y);
		if ((absDx <= 2 && absDy <= 2) && (absDx == 2 || absDy == 2 || (absDx == 0 && absDy == 0))) 
		{
			return true;
		}
		return false;
	}

	public bool playerInNoRoZone(){

		Int2 playerPos = blockManager.player.GetRoundedPosition();
		if (noRotationManager.hasNoRotationZone(playerPos)) {
			return true;
		}
		return false;

	}

    public void WinLevel()
    {
		int loadedLevel = Application.loadedLevel;
        if (loadedLevel < Application.levelCount - 1)
        {
            Application.LoadLevel(loadedLevel + 1);
        }
        else
        {
            Application.LoadLevel(0);
        }
    }

    public void ResetLevel()
    {
        Application.LoadLevel(Application.loadedLevel);
    }


}
