﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

public class LevelEditor : MonoBehaviour
{
	enum ToolMode { point = 0, select = 1 };
    ToolMode toolMode = ToolMode.point;
    public Texture point;
    public Texture line;
    public Texture rect;
	public Texture select;
    Texture[] toolImages;

    GameManager gameManager;
    BlockManager blockManager;
    NoRotationManager noRoMan;
    Player player;
	AbstractBlock selectedBlock;
	bool selectedPlayer;

    GameObject noRoPrefab;
	public GameObject selectionHighlightPrefab;
	GameObject selectionHighlight;

    string path;

    [System.Serializable]
    public class Brush
    {
        public string name;
        public Texture image;
        public GameObject prefab;
        public bool isPlayer;
        public bool isButter;
        public bool isNoRotationZone;
        public bool isCrawler;
    }

    public Brush[] brushes;
    Texture[] brushImages;
    int currentBrushNumber = 0;
    Brush currentBrush
    {
        get
        {
            return brushes[currentBrushNumber];
        }
    }

    HashSet<Rect> guiRects;


    void Awake()
    {
        this.gameManager = GetComponent<GameManager>();
        this.toolImages = new Texture[] { this.point, this.select};
        this.player = FindObjectOfType<Player>();
        this.blockManager = FindObjectOfType<BlockManager>();
        this.noRoMan = FindObjectOfType<NoRotationManager>();
		this.selectionHighlight = Instantiate (selectionHighlightPrefab) as GameObject;
		selectionHighlight.SetActive (false);
        guiRects = new HashSet<Rect>();

        // Get the images for our brushes
        this.brushImages = new Texture[brushes.Length];
        for (int i = 0; i < brushes.Length; i++)
        {
            brushImages[i] = brushes[i].image;
            // Set up our no rotation zone prefab
            if (brushes[i].isNoRotationZone)
            {
                this.noRoPrefab = brushes[i].prefab;
            }
        }
        this.path = Application.dataPath;
    }

    bool MouseInGUI()
    {
        // Because different coordinate systems for GUI and world
        Vector2 pos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        foreach (Rect r in guiRects)
        {
            if (r.Contains(pos))
            {
                return true;
            }
        }
        return false;
    }

    void Update()
    {
        Vector3 mouseVector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Int2 mouseWorldPos = new Int2(mouseVector.x, mouseVector.y);

        if (gameManager.gameState == GameManager.GameMode.editing)
        {
            if (!MouseInGUI())
            {
                switch (toolMode)
                {
                    case ToolMode.point:
	                {
						selectionHighlight.SetActive(false);
	                    if (currentBrush.isPlayer)
	                    {
	                        if (Input.GetMouseButton(0))
	                        {
	                            // If there's not a block where we're trying to place the player
	                            if (!blockManager.grid.ContainsKey(mouseWorldPos))
	                            {
	                                player = FindObjectOfType<Player>();
	                                if (player == null)
	                                {
                                        Instantiate(currentBrush.prefab, mouseWorldPos.ToVector2(), Quaternion.identity);
										blockManager.player = FindObjectOfType<Player>();
										gameManager.player = FindObjectOfType<Player>();
										gameManager.playerMovement = FindObjectOfType<PlayerMovement>();
									}
	                                else
	                                {
	                                    player.transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, player.transform.position.z);
	                                }
	                            }
	                        }
	                    }
	                    else if (currentBrush.isButter)
	                    {
	                        if (Input.GetMouseButton(0))
	                        {
	                            ButterBlock butter = FindObjectOfType<ButterBlock>();
	                            if (butter == null)
	                            {
                                    if (blockManager.getBlockAt(mouseWorldPos) == null)
                                    {
                                        GameObject b = Instantiate(currentBrush.prefab, mouseWorldPos.ToVector2(), Quaternion.identity) as GameObject;
                                        blockManager.AddBlock(mouseWorldPos, b.GetComponent<ButterBlock>());
                                    }
	                            }
	                            else
	                            {
                                    if (blockManager.getBlockAt(mouseWorldPos) == null)
                                    {
                                        blockManager.ChangePos(butter.GetCurrentPosition(), mouseWorldPos);
                                    }
	                            }
	                        }
                            else if (Input.GetMouseButton(1))
                            {
                                blockManager.RemoveBlock(mouseWorldPos);
                            }
	                    }
	                    else if (currentBrush.isCrawler)
	                    {
	                        if (Input.GetMouseButtonDown(0))
	                        {
	                            Instantiate(currentBrush.prefab, mouseWorldPos.ToVector2(), Quaternion.identity);
	                        }
	                    }
	                    else if (currentBrush.isNoRotationZone)
	                    {
	                        if (Input.GetMouseButton(0))
	                        {
                                noRoMan.AddNoRoZone(mouseWorldPos, currentBrush.prefab);
	                        }
	                        if (Input.GetMouseButton(1))
	                        {
	                            noRoMan.RemoveNoRoZone(mouseWorldPos);
	                        }
	                    }
	                    // Brush is a block
	                    else
	                    {
	                        if (Input.GetMouseButton(0))
	                        {
	                            if (player == null || !mouseWorldPos.Equals(player.GetRoundedPosition()))
	                            {
                                    CreateBlock(mouseWorldPos, currentBrush.prefab);
	                            }
	                        }
	                        else if (Input.GetMouseButton(1))
	                        {
	                            blockManager.RemoveBlock(mouseWorldPos);
	                        }
	                    }
	                    break;
	                }

					//Select Mode!
					case ToolMode.select:
					{
						//release left click = select this block 
						//click elsewhere = unselect block, move selected cursor there
						if(Input.GetMouseButtonUp(0))
						{
							
							selectedBlock = blockManager.getBlockAt(mouseWorldPos.x,mouseWorldPos.y);
							selectedPlayer = false;
							selectionHighlight.SetActive(true);
							selectionHighlight.transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, selectionHighlight.transform.position.z);

							if(player != null && selectedBlock ==null && player.GetRoundedPosition().x == mouseWorldPos.x && player.GetRoundedPosition().y == mouseWorldPos.y)
							{
								selectedPlayer = true;
							}
							
						}

						//if have a thing
						if(selectedBlock!=null || selectedPlayer)
						{
						//hold right click = drag this thing around
							if(Input.GetMouseButton(1)&&blockManager.getBlockAt(mouseWorldPos.x,mouseWorldPos.y)==null && (player == null || !mouseWorldPos.Equals(player.GetRoundedPosition())))
						    {
								//if holding block and there's no block or player there, 
								if(selectedBlock !=null )
								{
									blockManager.grid.Remove(selectedBlock.GetCurrentPosition());
									selectedBlock.transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y,0);
									selectionHighlight.transform.position = selectedBlock.transform.position;

									blockManager.grid.Add (new Int2(mouseWorldPos.x, mouseWorldPos.y), selectedBlock);
									if (selectedBlock as LaserShooter != null) {
										(selectedBlock as LaserShooter).setFireDirection();
									}
									else if (selectedBlock as MirrorBlock != null) {
										(selectedBlock as MirrorBlock).stopFiring();
									}

	 							}
								else if(selectedPlayer)
								{
									player.transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y,0);
									selectionHighlight.transform.position = player.transform.position;
								}
							}
							//rotate ccw
							else if(selectedBlock !=null && (Input.GetKeyDown(KeyCode.A) || Input.GetAxis("Mouse ScrollWheel") > 0))
							{
								selectedBlock.orientation += 1;
								selectedBlock.blockSprite.transform.eulerAngles = new Vector3(0f, 0f, selectedBlock.orientation * 90f);
								if (selectedBlock as LaserShooter != null) {
									(selectedBlock as LaserShooter).setFireDirection();
									
								}
								else if (selectedBlock as MirrorBlock != null) {
									(selectedBlock as MirrorBlock).stopFiring();
								}
							}
							else if(selectedBlock !=null && (Input.GetKeyDown(KeyCode.D) || Input.GetAxis("Mouse ScrollWheel") < 0))
							{
								selectedBlock.orientation -= 1;
								selectedBlock.blockSprite.transform.eulerAngles = new Vector3(0f, 0f, selectedBlock.orientation * 90f);
								if (selectedBlock as LaserShooter != null) {
									(selectedBlock as LaserShooter).setFireDirection();
								}
								else if (selectedBlock as MirrorBlock != null) {
									(selectedBlock as MirrorBlock).stopFiring();
								}
							}

						}
						break;

					}
				}
			}
		}
    }

    private void CreateBlock(Int2 mouseWorldPos, GameObject blockPrefab)
    {
        GameObject b = Instantiate(blockPrefab, mouseWorldPos.ToVector2(), Quaternion.identity) as GameObject;
        AbstractBlock theBlock = b.GetComponent<AbstractBlock>();
        blockManager.AddBlock(mouseWorldPos, theBlock);
    }

    void OnGUI()
    {
        float boxWidth = Screen.width / 3;
        float boxHeight = Screen.height / 10;
        Rect brushRect = new Rect(0, Screen.height - boxHeight, Screen.width, boxHeight);
        Rect toolRect = new Rect(0, 0, boxWidth, boxHeight);
        Rect playEditRect = new Rect(Screen.width - boxWidth, 0, boxWidth, boxHeight);
        Rect saveLoadRect = new Rect(Screen.width - boxWidth, boxHeight * 2, boxWidth, boxHeight * 2);
        // Clear our GUI rectangles, then add our current ones
        guiRects = new HashSet<Rect>();
        guiRects.Add(brushRect);
        guiRects.Add(toolRect);
        guiRects.Add(playEditRect);
        guiRects.Add(saveLoadRect);
        if (gameManager.gameState == GameManager.GameMode.editing)
        {
            // Different brushes
            GUILayout.BeginArea(brushRect);
            this.currentBrushNumber = GUILayout.Toolbar(currentBrushNumber, brushImages, GUILayout.MaxHeight(boxHeight), GUILayout.MaxWidth(Screen.width));
            GUILayout.EndArea();

            // Different tools
            GUILayout.BeginArea(toolRect);
            this.toolMode = (ToolMode)GUILayout.Toolbar((int)toolMode, toolImages, GUILayout.MaxWidth(boxWidth), GUILayout.MaxHeight(boxHeight));
            GUILayout.EndArea();

            // Play/Edit button
            GUILayout.BeginArea(playEditRect);
            if (GUILayout.Button("Play"))
            {
                selectionHighlight.SetActive(false);
                gameManager.gameState = GameManager.GameMode.playing;
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(saveLoadRect);
            if (GUILayout.Button("Save"))
            {
                LevelSkeleton currentLevel = this.ConvertLevelToSkeleton();
                LevelEditor.WriteXML(currentLevel, this.path);
            }
            if (GUILayout.Button("Load"))
            {
                LevelSkeleton loadedLevel = ReadXML(this.path);
            }
            GUILayout.EndArea();
        }
        else if (gameManager.gameState == GameManager.GameMode.playing && gameManager.canEdit)
        {
            GUILayout.BeginArea(playEditRect);
            if (GUILayout.Button("Edit"))
            {
                gameManager.gameState = GameManager.GameMode.editing;
            }
            GUILayout.EndArea();
        }
        
    }

    LevelSkeleton ConvertLevelToSkeleton()
    {
        LevelSkeleton skelly = new LevelSkeleton();
        skelly.setGrid(blockManager.grid);
        skelly.setNoRoZoneGrid(noRoMan.noRotationZones);
        skelly.setCrawlers();
        if (player != null)
        {
            skelly.playerPosition = player.GetRoundedPosition();
        }
        return skelly;
    }

    public static void WriteXML(LevelSkeleton level, string path)
    {
        level.playerPosition = new Int2(3, 5);
        XmlSerializer writer = new XmlSerializer(typeof(LevelSkeleton));
        System.IO.StreamWriter file = new System.IO.StreamWriter(path);
        Debug.Log("Wrote level to " + path);
        writer.Serialize(file, level);
        file.Close();
    }

    public static LevelSkeleton ReadXML(string path)
    {
       XmlSerializer deserializer = new XmlSerializer(typeof(LevelSkeleton));
       TextReader textReader = new StreamReader(path);
       LevelSkeleton loadedLevel;
       loadedLevel = (LevelSkeleton)deserializer.Deserialize(textReader);
       textReader.Close();
       return loadedLevel;
    }
}
