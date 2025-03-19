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
	public void OnMouseDown() {
		// --- NEW: Local checks to avoid picking up the wrong piece ---
		if (!NetworkPlayer.LocalInstance.IsMyTurn())
		{
			// Not my turn at all
			return;
		}
		// Are we white or black?
		bool iAmWhite = NetworkPlayer.LocalInstance.IsWhite.Value;
		// If I'm white but this piece is black => block
		if (iAmWhite && PieceColor == Side.Black) return;
		// If I'm black but this piece is white => block
		if (!iAmWhite && PieceColor == Side.White) return;
		// --- end NEW checks ---

		if (enabled) {
			piecePositionSS = boardCamera.WorldToScreenPoint(transform.position);
		}
	}

	/// <summary>
	/// Called while the user drags the piece with the mouse.
	/// Updates the piece's world position to follow the mouse cursor.
	/// </summary>
	private void OnMouseDrag() {
		if (enabled) {
			// Create a new screen-space position based on the current mouse position,
			// preserving the original depth (z-coordinate).
			Vector3 nextPiecePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
			// Convert the screen-space position back to world-space and update the piece's position.
			thisTransform.position = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);
		}
	}

	/// <summary>
	/// Called when the user releases the mouse button after dragging the piece.
	/// Determines the closest board square to the piece and raises an event with the move.
	/// </summary>
	public void OnMouseUp()
	{
		if (!enabled) return;

		// Find potential landing squares
		potentialLandingSquares.Clear();
		BoardManager.Instance.GetSquareGOsWithinRadius(
			potentialLandingSquares, thisTransform.position, SquareCollisionRadius
		);

		// If no squares found, reset piece to original position
		if (potentialLandingSquares.Count == 0)
		{
			thisTransform.position = thisTransform.parent.position;
			return;
		}

		// Find the closest square
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

		// Convert old & new squares into Vector2Int
		// old = current parent square
		Square oldSquare = StringToSquare(transform.parent.name);
		Vector2Int from = new Vector2Int(oldSquare.File, oldSquare.Rank);

		// new = closest square
		Square newSquare = new Square(closestSquareTransform.name);
		Vector2Int to = new Vector2Int(newSquare.File, newSquare.Rank);

		// ******* NETCODE CALL *******
		// Instead of local GameManager.OnPieceMoved(...),
		// call the ServerRpc in your NetworkChessManager.
		NetworkChessManager.Instance.RequestMoveServerRpc(from, to);
	}
}
