# Contract: BasicRigidBodyPush

Role: character push; applies a horizontal impulse to non-kinematic rigidbodies the character collides with, filtered by layer.

Fields:
pushLayers:LayerMask  the layers allowed to be pushed
canPush:bool  master switch
strength:float  [Range(0.5, 5)] impulse multiplier

Methods:
OnControllerColliderHit(ControllerColliderHit)  -> pushes when canPush
PushRigidBodies(hit)  skips kinematic bodies, filtered layers and objects the character stands on -> body.AddForce(horizontal * strength, Impulse)

Notes:
- The push direction is horizontal only (moveDirection.y is zeroed).
- Collisions coming from below (moveDirection.y < -0.3) are ignored so the character does not launch objects it is standing on.
