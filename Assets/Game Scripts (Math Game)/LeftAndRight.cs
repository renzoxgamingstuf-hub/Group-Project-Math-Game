using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeftAndRight : MonoBehaviour
{
private Rigidbody2D rb;
private BoxCollider2D coll;
private SpriteRenderer sprite;
private Animator anim;

private float dirX = 0f;
[SerializeField] private float moveSpeed = 7f;

private enum MovementState { Idle, Walk }

    // Start is called before the first frame update
    private void Start()
    {
rb = GetComponent<Rigidbody2D>();
coll = GetComponent<BoxCollider2D>();
sprite = GetComponent<SpriteRenderer>();
anim = GetComponent<Animator>();
}

    private void Update()
    {
dirX = Input.GetAxis("Horizontal");
rb.linearVelocity = new Vector2(dirX * moveSpeed, rb.linearVelocity.y);

UpdateAnimationState();
}

private void UpdateAnimationState()
{
MovementState state;

if (dirX > 0f)
{
state = MovementState.Walk;
sprite.flipX = false;
}
else if (dirX < 0f)
{
state = MovementState.Walk;
sprite.flipX = true;
}
else
{
state = MovementState.Idle;
}

anim.SetInteger("State", (int)state);
}

}
