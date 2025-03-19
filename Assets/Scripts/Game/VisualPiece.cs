using System.Collections.Generic;
using UnityChess;
using UnityEngine;
using static UnityChess.SquareUtil;

/// <summary>
/// Represents a visual chess piece in the game. This component handles user interaction,
/// such as dragging and dropping pieces, and determines the closest square on the board
/// where the piece should land. It also raises an event when a piece has been moved.
/// </summary>
public class VisualPiece : MonoBehaviour {
	// Delegate for handling the event when a visual piece has been moved.
	// Parameters: the initial square of the piece, its transform, the closest square's transform,
	// and an optional promotion piece.
	public delegate void VisualPieceMovedAction(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null);
	
	// Static event raised when a visual piece is moved.
	public static event VisualPieceMovedAction VisualPieceMoved;
	
	// The colour (side) of the piece (White or Black).
	public Side PieceColor;
	
	// Retrieves the current board square of the piece by converting its parent's name into a Square.
	public Square CurrentSquare => StringToSquare(transform.parent.name);
	
	// The radius used to detect nearby board squares for collision detection.
	private const float SquareCollisionRadius = 9f;
	
	// The camera used to view the board.
	private Camera boardCamera;
	// The screen-space position of the piece when it is first picked up.
	private Vector3 piecePositionSS;
	// A reference to the piece's SphereCollider (if required for collision handling).
	private SphereCollider pieceBoundingSphere;
	// A list to hold potential board square GameObjects that the piece might land on.
	private List<GameObject> potentialLandingSquares;
	// A cached reference to the transform of this piece.
	private Transform thisTransform;

	/// <summary>
	/// Initialises the visual piece. Sets up necessary variables and obtains a reference to the main camera.
	/// </summary>
	private void Start() {
		// Initialise the list to hold potential landing squares.
		potentialLandingSquares = new List<GameObject>();
		// Cache the transform of this GameObject for efficiency.
		thisTransform = transform;
		// Obtain the main camera from the scene.
		boardCamera = Camera.main;
	}

	/// <summary>
	/// Called when the user presses the mouse button over the piece.
	/// Records the initial screen-space position of the piece.
	/// </summary>
	public void OnMouseDown()
	{
		// If the script is disabled, OnMouseDown still fires but we can bail out quickly
		if (!enabled) return;

		if (NetworkPlayer.LocalInstance == null) return;

		// Check local turn logic
		bool myTurn = NetworkPlayer.LocalInstance.IsMyTurn();
		bool iAmWhite = NetworkPlayer.LocalInstance.IsWhite.Value;
		if (!myTurn) return; // Not my turn
		if (iAmWhite && PieceColor == Side.Black) return; // Wrong color
		if (!iAmWhite && PieceColor == Side.White) return; // Wrong color

		// We can pick up the piece
		piecePositionSS = boardCamera.WorldToScreenPoint(transform.position);
	}

	/// <summary>
	/// Called while the user drags the piece with the mouse.
	/// Updates the piece's world position to follow the mouse cursor.
	/// </summary>
	private void OnMouseDrag()
	{
		// If script is disabled, do nothing
		if (!enabled) return;
		if (Input.GetMouseButton(0))
		{
			Vector3 nextPosSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
			thisTransform.position = boardCamera.ScreenToWorldPoint(nextPosSS);
		}
	}

	/// <summary>
	/// Called when the user releases the mouse button after dragging the piece.
	/// Determines the closest board square to the piece and raises an event with the move.
	/// </summary>
	public void OnMouseUp()
	{
		if (!enabled) return;

		potentialLandingSquares.Clear();
		BoardManager.Instance.GetSquareGOsWithinRadius(
			potentialLandingSquares, thisTransform.position, SquareCollisionRadius
		);

		if (potentialLandingSquares.Count == 0)
		{
			// No squares => reset
			thisTransform.position = thisTransform.parent.position;
			return;
		}

		// Find closest square
		Transform closestSquareTransform = potentialLandingSquares[0].transform;
		float shortestDistSqr = (closestSquareTransform.position - thisTransform.position).sqrMagnitude;
		for (int i = 1; i < potentialLandingSquares.Count; i++)
		{
			float distSqr = (potentialLandingSquares[i].transform.position - thisTransform.position).sqrMagnitude;
			if (distSqr < shortestDistSqr)
			{
				shortestDistSqr = distSqr;
				closestSquareTransform = potentialLandingSquares[i].transform;
			}
		}

		// Convert to Vector2Int
		Square oldSquare = StringToSquare(transform.parent.name);
		Vector2Int from = new Vector2Int(oldSquare.File, oldSquare.Rank);

		Square newSquare = new Square(closestSquareTransform.name);
		Vector2Int to = new Vector2Int(newSquare.File, newSquare.Rank);

		// Send to server
		NetworkChessManager.Instance.RequestMoveServerRpc(from, to);
	}
}
