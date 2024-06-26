using MenteBacata.ScivoloCharacterController;
using MenteBacata.ScivoloCharacterControllerDemo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    public bool preventMoving = false;

    public bool preventRotation = false;

    public Animator anim;

    public LayerMask collisionLayer;

    public float distanceToGround;

    [SerializeField] private CharacterIK characterIK;

    [SerializeField] private CharacterMover mover;

    [SerializeField] private SimpleCharacterController characterController;

    [SerializeField] private CharacterCapsule characterCapsule;

    [SerializeField] private float normalHeight;

    [SerializeField] private float crouchedHeight;

    [SerializeField] private float blendSmoothness = 5f;

    [SerializeField] private PlayerValues playerValues;

    [SerializeField] private float staminaSpent;

    [SerializeField] private ClimbManager climbManager;

    [SerializeField] private PlaneManager planeManager;


    private void Update()
    {
        CalculateBlendTreeValues();

        characterController.canMove = !preventMoving;
        characterController.canRotate = !preventRotation;
    }

    void LateUpdate()
    {
        anim.SetBool("OnClimbMode", climbManager.onClimbMode);

        preventMoving = characterIK.preventMoving;
        preventRotation = characterIK.preventRotation;

        anim.SetBool("Air", !mover.isInWalkMode);

        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            anim.SetBool("Walk", true);
        }
        else
        {
            anim.SetBool("Walk", false);
            anim.SetBool("Run", false);
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            characterCapsule.Height = crouchedHeight;
            anim.SetBool("Crouched", true);
            characterController.realMoveSpeed = characterController.slowMoveSpeed;
        }
        else if (!Physics.Raycast(transform.position, transform.up, normalHeight - 0.1f, collisionLayer))
        {
            characterController.realMoveSpeed = characterController.normalMoveSpeed;
            anim.SetBool("Crouched", false);
            characterCapsule.Height = normalHeight;

            if (Input.GetKey(KeyCode.LeftShift) && playerValues.stamina > 0 && !climbManager.onClimbMode)
            {
                characterController.realMoveSpeed = characterController.fastMoveSpeed;
                if (anim.GetBool("Walk") && !planeManager.planing && !climbManager.onClimbMode)
                {
                    playerValues.stamina -= Time.deltaTime * staminaSpent;
                }
                anim.SetBool("Run", true);
            }
            else
            {
                anim.SetBool("Run", false);
            }
        }

        playerValues.canRecoverStamina = !anim.GetBool("Run");
        
        if (Input.GetKey(KeyCode.LeftShift) || climbManager.onClimbMode)
        {
            playerValues.canRecoverStamina = false;
        }

        if (climbManager.onClimbMode)
        {
            Climb();
        }
    }

    private void Climb()
    {
        characterIK.IKLeftFootWeight = 0;
        characterIK.IKRightFootWeight = 0;
    }

    private void CalculateBlendTreeValues()
    {
        // Obtener la velocidad del personaje en el plano XZ
        Vector3 velocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
        velocity.Normalize();

        // Obtener la direcci�n del personaje en el plano XZ
        Vector3 characterDirection = new Vector3(transform.forward.x, 0f, transform.forward.z);
        characterDirection.Normalize();

        // Calcular el �ngulo entre la velocidad y la direcci�n
        float angle = Vector3.SignedAngle(characterDirection, velocity, Vector3.up);

        // Convertir el �ngulo en una direcci�n de blend tree
        Vector2 blendDirection = AngleToBlendDirection(angle);

        // Interpolar suavemente entre la direcci�n actual y la direcci�n del blend tree
        Vector2 smoothBlendDirection = Vector2.Lerp(GetCurrentBlendDirection(), blendDirection, blendSmoothness * Time.deltaTime);

        // Pasar la diferencia al blend tree
        anim.SetFloat("YDir", smoothBlendDirection.y);
        anim.SetFloat("XDir", smoothBlendDirection.x);
    }

    private Vector2 GetCurrentBlendDirection()
    {
        float yDir = anim.GetFloat("YDir");
        float xDir = anim.GetFloat("XDir");
        return new Vector2(xDir, yDir);
    }

    private Vector2 AngleToBlendDirection(float angle)
    {
        const float forwardThreshold = 45f;
        const float backwardThreshold = 135f;

        if (angle >= -forwardThreshold && angle <= forwardThreshold)
        {
            return new Vector2(0f, 1f); // Hacia adelante
        }
        else if (angle >= backwardThreshold || angle <= -backwardThreshold)
        {
            return new Vector2(0f, -1f); // Hacia atr�s
        }
        else if (angle > forwardThreshold && angle < backwardThreshold)
        {
            return new Vector2(1f, 0f); // Hacia la derecha
        }
        else
        {
            return new Vector2(-1f, 0f); // Hacia la izquierda
        }   
    }

    public void Jump()
    {
        anim.SetTrigger("Jump");
        Invoke("ResetJump", 0.1f);
    } 
    private void ResetJump() { anim.ResetTrigger("Jump"); }
}
