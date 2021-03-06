﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Public member in SceneManager.

public class HighLightSquareByGrid : MonoBehaviour
{
	public Dropdown mouseBehavior;
	public int mouseBehaviorIndex;

	public ChessBoard3D chessBoard;
	public PlanesSelection planesSelection; // Dropdown script.
	public HighlightRookPlanes highlightRookPlanes;
	public HighlightBishopPlanes highlightBishopPlanes;

	// Used to highlight planes.
	Vector3Int nullSquare = new Vector3Int(-1, -1, -1); // TODO: make const.
	Vector3Int prevSquare;

	// Used to highlight advancement squares.
	Vector3Int srcSquare;
	Vector3Int dstSquare;

	private bool debug;

	List<AdvancementSquare> advSqs;
	List<List<AdvancementSquare>> advSqSets = new List<List<AdvancementSquare>>();

	public void MouseBehaviorDropdownIndexChanged(int index)
	{
		mouseBehaviorIndex = index; // TODO: So we can see in Inspector at runtime - not working.
		print("Mouse behavior dropdown index = " + mouseBehaviorIndex);
		print("  MouseBehavior dropdown = " + mouseBehavior.captionText.text);
	}

	// Start is called before the first frame update
	void Start()
	{
		print("----- HighLightSquareByGrid.Start() -----");
		print("  Initial chessBoard.size = " + chessBoard.size);
		print("  Initial chessBoard.chessBoardProperties.size = " + chessBoard.properties.size);
		prevSquare = nullSquare;

		srcSquare = nullSquare;
		dstSquare = nullSquare;

		advSqs = new List<AdvancementSquare>();

		print("  Size of advSqs = " + advSqs.Count);
	}

	Vector3Int FindRaycastSquare(RaycastHit hit)
	{
		Vector3 point = hit.point;

		// Remember that chessboard coordiantes are permutation of Unity coordinates.
		ChessBoardProperties props = chessBoard.properties; // New, SRP compliant.

		Vector2 boardXedges = props.boardXedges;
		Vector2 boardYedges = props.boardYedges;
		Vector2 boardZedges = props.boardZedges;
		Vector3Int size = props.size;
		float squareThickness = props.squareThickness;
		int boardVerticalSep = props.boardVerticalSep;

		if(debug) {
			print("boardXedges = " + boardXedges);
			print("boardYedges = " + boardYedges);
			print("boardZedges = " + boardZedges);
			print("size = " + size);
			print("squareThickness = " + squareThickness);
			print("boardVerticalSep = " + boardVerticalSep);
			print("----------");
		}

		bool withinX = boardXedges[0] < point.z && point.z < boardXedges[1];
		bool withinY = boardYedges[0] < point.x && point.x < boardYedges[1];
		bool withinZ = boardZedges[0] < point.y && point.y < boardZedges[1] + squareThickness;

		if (!(withinX && withinY && withinZ)) {
			print("*** nullSquare ***");
			return nullSquare;
		}

		int zCoord = 0;
		int xCoord = 0;
		int yCoord = 0;

		// Find level (horizontal).
		for (int k = 0; k < size.z; k++) {
			float loZ = boardZedges[0] + k * boardVerticalSep;
			float hiZ = boardZedges[0] + (k + 1 - squareThickness) * boardVerticalSep;
			withinZ = loZ < point.y && point.y < hiZ;
			if (withinZ) {
				zCoord = k;
				break;
			}
		}
		if (!withinZ) {
			return nullSquare;
		}

		// Find left vertical.
		for (int i = 0; i < size.x; i++) {
			withinX = boardXedges[0] + i < point.z && point.z < boardXedges[0] + i + 1;
			if (withinX) {
				xCoord = i;
				break;
			}
		}

		// Find right vertical.
		for (int j = 0; j < size.y; j++) {
			withinY = boardYedges[0] + j < point.x && point.x < boardYedges[0] + j + 1;
			if (withinY) {
				yCoord = j;
				break;
			}
		}

		return new Vector3Int(xCoord, yCoord, zCoord);
	}

	bool frozen = true;

	// Update is called once per frame
	void Update()
	{
		// TODO: Inefficient, should only query when it changes.
		switch (mouseBehavior.captionText.text) {
		case "Select Mouse Behavior":
			break;
		case "Source/Destination":
			SetSrcDstSquares();
			break;

		case "Highlight Planes on Mouseover":
			HighlightPlanes();
			break;
		case "Animate Advancement Squares":
			ManageAdvSqs();
			break;

		default:
			print("Unknown Mouse Behavior dropdown option - " + mouseBehavior.captionText.text);
			break;
		}
	}

	/* Logic for creating/showing/changing/clearing advancement squares:
	 * Filtered by which planes are selected (rook, bishop, duke, queen, etc.).
	 * 
	 * With srcSq on board:
	 *   Fresh dstSq makes advSq.
	 *   Toggle clears advSq.
	 *   Overriding srcSq locks the advancement square - keeps it on the board.
	 *   Move has two possibilities:
	 *     If in different advSq, clear the previous, and show the new one.
	 *     If in same advSq, add perimeters or clear them as appropriate.
	 *
	 * With dstSq on board:
	 *   Fresh srcSq makes advSq.
	 *   Toggle clears advSq.
	 *   Overriding dstSq locks the advancement square - keeps it on the board.
	 *   Move has two possibilities:
	 *     If in different advSq, clear previous and show new one.
	 *     If in same advSq, shift, shrink or grow advancement square, but no perimeter by perimeter presentation.
	 *
	 * TODO: Replace prints with actual advSq constructions and changes.
	 */

	List<AdvancementSquare> CreateAdvSqs(Vector3Int srcSq, Vector3Int dstSq)
	{
		List<AdvancementSquare> advSqrs = new List<AdvancementSquare>();

		string plane = planesSelection.selectedPlane.text;

		if (planesSelection.rookPlanes.Contains(plane)) {
			List<AdvancementSquare> rookAdvSqs = highlightRookPlanes.AdvSqs(srcSq, dstSq);
			for(int i=0; i<rookAdvSqs.Count; i++) {
				advSqrs.Add(rookAdvSqs[i]);
			}
		}
		// Seampoint - add bishop, duke, etc.

		// Visual diagnostic only.
		for(int i=0; i<advSqrs.Count; i++) {
			print("[][][][][]");
		}

		return advSqrs;
	}

	AdvancementSquare CreateAdvSq(Vector3Int srcSq, Vector3Int dstSq)
	{
		AdvancementSquare advSq = null;
		List<AdvancementSquare> advSqs = new List<AdvancementSquare>();

		string plane = planesSelection.selectedPlane.text;

		if (planesSelection.rookPlanes.Contains(plane)) {
			if ((advSq = highlightRookPlanes.AdvSq(srcSq, dstSq)) != null) {
				List<AdvancementSquare> testAdvSqs = new List<AdvancementSquare>();
				testAdvSqs.Add(advSq);
				testAdvSqs.Add(advSq);
				advSqs.AddRange(testAdvSqs);
			}
		}

		return advSq;
	}

	// Call as StartCoroutine(ClearAllAdvSqsInReverseOrder()).
	IEnumerator ClearAllAdvSqsInReverseOrder()
	{
		for (int i = advSqs.Count - 1; i >= 0; i--) {
			StartCoroutine(highlightRookPlanes.ClearAdvSqByPerimeter(advSqs[i]));
			yield return new WaitForSeconds(0.5f); // TODO: put advSqByPerimeter speed under player control.

			advSqs.RemoveAt(i);
			print("  Size of advSqs = " + advSqs.Count);
		}

	}

	bool lastAdvSqLocked;
	void SetSrcDstSquares() // Src/dst looks correct, complete & concise 9/28/19, but advSqs are wip.
	{
		Color srcColor = Color.yellow;
		Color dstColor = Color.magenta;

		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // From camera to mouse.
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit)) { // Ray hit a GameObject (square, piece, etc.).
			Vector3Int raycastSquare = FindRaycastSquare(hit);

			// Click marks source square.
			if (Input.GetMouseButtonUp(1)) {
				if (srcSquare == nullSquare) {              // Fresh srcSq.
					if (dstSquare == raycastSquare) {       // Overrides dstSq.
						dstSquare = Unhighlight(raycastSquare);
						print("Overrides dstSq.");
					}
					srcSquare = Highlight(raycastSquare, srcColor);
					if (dstSquare != nullSquare) {
						print("Show new advSq.");
					}
					lastAdvSqLocked = false;
				} else if (raycastSquare == srcSquare) {    // Toggle srcSq.
					srcSquare = Unhighlight(srcSquare);
					if (dstSquare != nullSquare) {
						print("Revert last advSq.");
					}
				} else {                                    // Move srcSq.
					srcSquare = Unhighlight(srcSquare);
					if (dstSquare == raycastSquare) {       // Lock advSq (srcSq ontop of dstSq).
						print("Lock last advSq.");
						lastAdvSqLocked = true;
					} else {
						srcSquare = Highlight(raycastSquare, srcColor);
						if (dstSquare != nullSquare) {
							// Mock code until can return a new advancement square:
							AdvancementSquare first = new AdvancementSquare();
							AdvancementSquare second = new AdvancementSquare();

							// Mock code, is only returning 'Different':
							MetaSet metaSet = AdvancementSquare.AreAdSqsMetaSet(first, second);
							if (metaSet == MetaSet.SubSet || metaSet == MetaSet.SuperSet || metaSet == MetaSet.ShiftSet) {
								print("Resize or shift advSq");
							} else if (metaSet == MetaSet.Null) {
								if (!lastAdvSqLocked) {
									print("Clear last advSq.");
								}
							} else if (metaSet == MetaSet.Different) {
								if (!lastAdvSqLocked) {
									print("Clear last advSq.");
								}
								print("Show next advSq.");
								lastAdvSqLocked = false;
							} else if (metaSet == MetaSet.Identical) {
								print("Identical should never happen - Do nothing.");
							}
						}
					}
				}
			}

			// Click marks destination square.
			if (Input.GetMouseButtonUp(0)) {
				if (dstSquare == nullSquare) {              // Fresh dstSq.
					if (srcSquare == raycastSquare) {		// Overrides srcSq.
						srcSquare = Unhighlight(srcSquare);
						print("Overrides srcSq.");
					}
					dstSquare = Highlight(raycastSquare, dstColor);
					if (srcSquare != nullSquare) {
						print("Show new advSq.");
						AdvancementSquare advSq = CreateAdvSq(srcSquare, dstSquare);
						if (advSq != null) {
							advSqs.Add(advSq);
							StartCoroutine(highlightRookPlanes.ShowAdvSqByPerimeter(advSq));
						}
						print("  Size of advSqs = " + advSqs.Count);
					}
					lastAdvSqLocked = false;
				}
				else if (raycastSquare == dstSquare) {		// Toggle dstSq.
					dstSquare = Unhighlight(dstSquare);
					if (srcSquare != nullSquare) {
						print("Revert last advSq.");
						int prevAdvSqIndex = advSqs.Count - 1;
						StartCoroutine(highlightRookPlanes.ClearAdvSqByPerimeter(advSqs[prevAdvSqIndex]));
						advSqs.RemoveAt(prevAdvSqIndex);
					}
				}
				else {										// Move dstSq.
					Unhighlight(dstSquare);
					if (srcSquare == raycastSquare) {		// Lock advSq (dstSq ontop of srcSq).
						print("Lock last advSq.");
						bool line = false;
						highlightRookPlanes.HighlightSquare(dstSquare, line);
						dstSquare = nullSquare;
						lastAdvSqLocked = true;
					} else {
						dstSquare = Highlight(raycastSquare, dstColor);
						if (srcSquare != nullSquare) {
							// Mock code until can return a new advancement square:
							AdvancementSquare first = new AdvancementSquare();
							AdvancementSquare second = new AdvancementSquare();

							// Mock code, is only returning 'Different':
							MetaSet metaSet = AdvancementSquare.AreAdSqsMetaSet(first, second);
							if (metaSet == MetaSet.SubSet || metaSet == MetaSet.SuperSet) {
								print("Resize advSq (extend or contract perimeters).");
							} else if (metaSet == MetaSet.Null) {
								if (!lastAdvSqLocked) {
									print("Clear last advSq.");
								}
							} else if (metaSet == MetaSet.Different) {
								int prevAdvSqIndex = advSqs.Count - 1;
								AdvancementSquare advSq1 = advSqs[prevAdvSqIndex];
								AdvancementSquare advSq2 = CreateAdvSq(srcSquare, dstSquare);
								if (advSq2 != null) {
									advSqs.Add(advSq2);
								}
								if (lastAdvSqLocked) {
									print("Show next advSq.");
									StartCoroutine(highlightRookPlanes.ShowAdvSqByPerimeter(advSq2));
								} else {
									print("Clear previous advSq and show next advSq.");
									StartCoroutine(highlightRookPlanes.ClearShowAdvSqsByPerimeter(advSq1, advSq2));
									advSqs.RemoveAt(prevAdvSqIndex);
									lastAdvSqLocked = false;
								}
							} else if (metaSet == MetaSet.Identical) {
								print("Do nothing.");
							}
						}
					}
				}
			}
		}
		else { // Clear src & dst squares off the board.
			if (Input.GetMouseButtonUp(1)) {
				if (srcSquare != nullSquare) {
					srcSquare = Unhighlight(srcSquare);
					print("Lock last advSq.");
					lastAdvSqLocked = true;
				}
				if (srcSquare == nullSquare && dstSquare == nullSquare) {
					print("--- Clear all advancement squares off the board.");
					StartCoroutine(ClearAllAdvSqsInReverseOrder());
				}
			}
			if (Input.GetMouseButtonUp(0)) {
				if (dstSquare != nullSquare) {
					dstSquare = Unhighlight(dstSquare);
					print("Lock last advSq.");
					lastAdvSqLocked = true;
				}
				if (srcSquare == nullSquare && dstSquare == nullSquare) {
					print("--- Clear all advancement squares off the board.");
				}
			}
		}
	}

	/* Todo list:
	 * Restoring dstSquare on lock via AdvSq.
	 * Restoring srcSquare on lock via AdvSq.
	 * Overlapping advancement squares.
	 * Same logic for moving the srcSquare around.
	 * Adjusting within an advancement square.
	 * Deprecating obsolete code.
	 */

	bool newAdvSq;
	bool clearAdvSq;

	void ManageAdvSqs()
	{
		Color srcColor = Color.yellow;
		Color dstColor = Color.magenta;

		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // From camera to mouse.
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit)) { // Ray hit a GameObject (square, piece, etc.).
			Vector3Int raycastSquare = FindRaycastSquare(hit);

			// Click marks source square: fresh, clobber, toggle(clear), move, or lock.
			if (Input.GetMouseButtonUp(1)) {
				if (srcSquare == nullSquare) {
					if (raycastSquare != dstSquare) {								// Fresh.
						print("Fresh srcSquare " + srcSquare +">"+ raycastSquare);
						if (dstSquare != nullSquare) print("Possibly advSq(s)");
					}
					else if (raycastSquare == dstSquare) {							// Clobber.
						print("Clobbers dstSquare " + srcSquare +">"+ dstSquare);
						dstSquare = Unhighlight(dstSquare);
					}
					srcSquare = Highlight(raycastSquare, srcColor);
				}
				else if (srcSquare == raycastSquare) {								// Toggle.
					print("Toggle srcSquare " + srcSquare);
					srcSquare = Unhighlight(srcSquare);
					print("Clear advSq(s)");
				}
				else {
					if (raycastSquare != dstSquare) {								// Move.
						print("Move srcSquare " + srcSquare +">"+ raycastSquare);
						print("Possibly clear advSq(s)");
						print("Possibly new or shift advSq(s)");
					}
					else if (raycastSquare == dstSquare) {							// Lock.
						print("Move clobbers dstSquare " + srcSquare +">"+ dstSquare);
						dstSquare = Unhighlight(dstSquare);
						print("Possibly lock advSq(s)");
					}
					Unhighlight(srcSquare);
					srcSquare = Highlight(raycastSquare, srcColor);
				}
			}

			// Click marks destination square: fresh, clobber, toggle (clear), move, or lock.
			if (Input.GetMouseButtonUp(0)) {
				if (dstSquare == nullSquare) {
					if (raycastSquare != srcSquare) {								// Fresh.
						print("Fresh dstSquare " + dstSquare +">"+ raycastSquare);
						if (srcSquare != nullSquare) {
							dstSquare = Highlight(raycastSquare, dstColor);
							advSqs = CreateAdvSqs(srcSquare, dstSquare);
							if (advSqs.Count > 0) {
								advSqSets.Add(advSqs);
								print(advSqs.Count + " new advSq(s).");
								newAdvSq = true;
							} else {
								print("Not in plane.");
							}
						}
					}
					else if(raycastSquare == srcSquare) {							// Clobber.
						print("Clobbers srcSquare " + dstSquare +">"+ srcSquare);
						srcSquare = Unhighlight(srcSquare);
						dstSquare = Highlight(raycastSquare, dstColor);
					}
				}
				else if (dstSquare == raycastSquare) {								// Toggle.
					print("Toggle dstSquare " + dstSquare);
					dstSquare = Unhighlight(dstSquare);
					print("Clear advSq(s)");
					clearAdvSq = true;
				}
				else {
					if (raycastSquare != srcSquare) {								// Move.
						print("Move dstSquare " + dstSquare +">"+ raycastSquare);
						// Adjust will take something else.
						print("Possibly adjust advSq(s)");
						print("Implementing clear and new advSq(s)");
						dstSquare = Unhighlight(dstSquare);
						dstSquare = Highlight(raycastSquare, dstColor);
						advSqs = CreateAdvSqs(srcSquare, dstSquare);
						if (advSqs.Count > 0) {
							advSqSets.Add(advSqs);
							print("Clear old advSq(s), show new.");
							clearAdvSq = newAdvSq = true;
						}
					}
					else if (raycastSquare == srcSquare) {							// Lock.
						print("Move clobbers srcSquare " + dstSquare +">"+ srcSquare);
						print("Possibly lock advSq(s)");
						Unhighlight(dstSquare);
						RestoreHighlighting(dstSquare, srcSquare);
						dstSquare = nullSquare;
					}
				}
				// Show last, clear last, adjust/shift last, clear prev and show last(current).
				StartCoroutine(ManageAdvSqSets());
			}
		}
		else { // Clear src and dst squares off the board; if both null, clear advSqs.
			if (Input.GetMouseButtonUp(1)) {
				if (srcSquare != nullSquare) {
					print("Clear board of srcSquare " + srcSquare +">"+ nullSquare);
					srcSquare = Unhighlight(srcSquare);
				}
				else if(dstSquare == nullSquare) {
					print("Clear board of all advSqs");
				}
			}
			if (Input.GetMouseButtonUp(0)) {
				if (dstSquare != nullSquare) {
					print("Clear board of dstSquare " + dstSquare + ">" + nullSquare);
					dstSquare = Unhighlight(dstSquare);
				}
				else if (srcSquare == nullSquare) {
					print("Clear board of all advSqs");
				}
			}
		}
	}

	void RestoreHighlighting(Vector3Int dstSq, Vector3Int srcSq)
	{
		print("Need to highlight dstSquare with proper advSq color & tinting.");

		string plane = planesSelection.selectedPlane.text; // TODO: get from advSq.

		if (planesSelection.rookPlanes.Contains(plane)) {
			highlightRookPlanes.RestoreHighlighting(dstSq, srcSq);
		}
	}

	private IEnumerator ManageAdvSqSets()
	{
		print("ManageAdvSqSets() " + advSqSets.Count);

		if (newAdvSq && clearAdvSq) { // Move.
			advSqs = advSqSets[advSqSets.Count - 2];
			for (int i = advSqs.Count - 1; i >= 0; i--) {
				// TODO: figure out types, am assuming rook here.
				StartCoroutine(highlightRookPlanes.ClearAdvSqByPerimeter(advSqs[i]));
				int perims = advSqs[i].perimeters.Count;
				float delayLaunch = perims * 2 * 0.1f; // TODO put under UI control.
				yield return new WaitForSeconds(delayLaunch);
			}
			advSqSets.Remove(advSqs);
			advSqs = advSqSets[advSqSets.Count - 1];
			for (int i = 0; i < advSqs.Count; i++) {
				// TODO: figure out types, am assuming rook here.
				StartCoroutine(highlightRookPlanes.ShowAdvSqByPerimeter(advSqs[i]));
				int perims = advSqs[i].perimeters.Count;
				float delayLaunch = perims * 2 * 0.1f; // TODO put under UI control.
				yield return new WaitForSeconds(delayLaunch);
			}
			clearAdvSq = newAdvSq = false;
		}
		else if (clearAdvSq) { // Toggle.
			advSqs = advSqSets[advSqSets.Count - 1];
			for (int i = advSqs.Count - 1; i >= 0; i--) {
				// TODO: figure out types, am assuming rook here.
				StartCoroutine(highlightRookPlanes.ClearAdvSqByPerimeter(advSqs[i]));
				int perims = advSqs[i].perimeters.Count;
				float delayLaunch = perims * 2 * 0.1f; // TODO put under UI control.
				yield return new WaitForSeconds(delayLaunch);
			}
			advSqSets.Remove(advSqs);
			clearAdvSq = false;
		}
		else if (newAdvSq) { // Fresh.
			advSqs = advSqSets[advSqSets.Count-1];
			for(int i=0; i<advSqs.Count; i++) {
				// TODO: figure out types, am assuming rook here.
				StartCoroutine(highlightRookPlanes.ShowAdvSqByPerimeter(advSqs[i]));
				int perims = advSqs[i].perimeters.Count;
				float delayLaunch = perims * 2 * 0.1f; // TODO put under UI control.
				yield return new WaitForSeconds(delayLaunch);
			}
			newAdvSq = false;
		}
	}

	private Vector3Int Highlight(Vector3Int sq, Color color)
	{
		chessBoard.cells[sq.x, sq.y, sq.z].HighlightCell(color);

		return sq;
	}

	private Vector3Int Unhighlight(Vector3Int sq)
	{
		chessBoard.cells[sq.x, sq.y, sq.z].UnhighlightCell();

		return nullSquare;
	}

	void HighlightPlanes()
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // From camera to mouse.

		string plane = planesSelection.selectedPlane.text;

		RaycastHit hit;
		if (Physics.Raycast(ray, out hit)) { // Ray hit a GameObject (square, piece, etc.).

			print("Plane selected = " + plane);

			// Find raycast square. The nullSquare (-1,-1,-1) means hit is not on any square.
			Vector3Int raycastSquare = FindRaycastSquare(hit);
			print("  Square hit = " + raycastSquare);

			// TODO: Add if's based on plane selection dropdown.
			if (raycastSquare == nullSquare) {
				if (!frozen) {
					if (prevSquare != nullSquare) { // Unhighlite prevSquare.
						if (planesSelection.rookPlanes.Contains(plane)) {
							highlightRookPlanes.HighlightPlanes(plane, prevSquare, Color.clear);
						}
						if (planesSelection.bishopPlanes.Contains(plane)) {
							highlightBishopPlanes.HighlightPlanes(plane, prevSquare, Color.clear);
						}
						// Seampoint - add other bases pieces.

						prevSquare = nullSquare;
					}
				}
			} else if (raycastSquare != prevSquare) {
				if (!frozen) {
					if (prevSquare != nullSquare) { // Unhighlite prevSquare.
						if (planesSelection.rookPlanes.Contains(plane)) {
							highlightRookPlanes.HighlightPlanes(plane, prevSquare, Color.clear);
						}
						if (planesSelection.bishopPlanes.Contains(plane)) {
							highlightBishopPlanes.HighlightPlanes(plane, prevSquare, Color.clear);
						}
						// Seampoint - add other bases pieces.
					}
					// Highlite raycastSquare.
					if (planesSelection.rookPlanes.Contains(plane)) {
						highlightRookPlanes.HighlightPlanes(plane, raycastSquare, Color.red);
					}
					if (planesSelection.bishopPlanes.Contains(plane)) {
						highlightBishopPlanes.HighlightPlanes(plane, raycastSquare, Color.green);
					}
					// Seampoint - add other bases pieces.

					prevSquare = raycastSquare;
				}
			}

		} else {
			if (!frozen) {
				if (prevSquare != nullSquare) { // Unhighlite prevSquare.
					if (planesSelection.rookPlanes.Contains(plane)) {
						highlightRookPlanes.HighlightPlanes(plane, prevSquare, Color.clear);
					}
					if (planesSelection.bishopPlanes.Contains(plane)) {
						highlightBishopPlanes.HighlightPlanes(plane, prevSquare, Color.clear);
					}
					// Seampoint - add other bases pieces.

					prevSquare = nullSquare;
				}
			}
		}

		if (Input.GetMouseButtonUp(0)) {
			frozen = !frozen;
		}
	}
}
