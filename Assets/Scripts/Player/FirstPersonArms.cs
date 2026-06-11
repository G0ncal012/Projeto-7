using UnityEngine;

/// <summary>
/// Levanta os braços do modelo (rig Humanoid) para uma pose "a segurar à frente da câmara",
/// para que apareçam na vista em primeira pessoa. Aplica-se depois do Animator (LateUpdate),
/// por isso sobrepõe-se a qualquer animação de idle/movimento.
/// </summary>
public class FirstPersonArms : MonoBehaviour
{
    [Header("Pose - Braço Direito")]
    [SerializeField] private Vector3 rightUpperArmEuler = new Vector3(60f, 0f, 10f);
    [SerializeField] private Vector3 rightLowerArmEuler = new Vector3(40f, 0f, 0f);
    [SerializeField] private Vector3 rightHandEuler = new Vector3(0f, 0f, 0f);

    [Header("Pose - Braço Esquerdo")]
    [SerializeField] private Vector3 leftUpperArmEuler = new Vector3(60f, 0f, -10f);
    [SerializeField] private Vector3 leftLowerArmEuler = new Vector3(40f, 0f, 0f);
    [SerializeField] private Vector3 leftHandEuler = new Vector3(0f, 0f, 0f);

    [Tooltip("Se ativo, aplica a pose dos braços todos os frames.")]
    [SerializeField] private bool applyPose = true;

    private Animator animator;

    private Transform rightUpperArm, rightLowerArm, rightHand;
    private Transform leftUpperArm, leftLowerArm, leftHand;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("[FirstPersonArms] Nenhum Animator encontrado no player.");
            enabled = false;
            return;
        }

        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
    }

    private void LateUpdate()
    {
        if (!applyPose) return;

        if (rightUpperArm != null) rightUpperArm.localRotation = Quaternion.Euler(rightUpperArmEuler);
        if (rightLowerArm != null) rightLowerArm.localRotation = Quaternion.Euler(rightLowerArmEuler);
        if (rightHand != null) rightHand.localRotation = Quaternion.Euler(rightHandEuler);

        if (leftUpperArm != null) leftUpperArm.localRotation = Quaternion.Euler(leftUpperArmEuler);
        if (leftLowerArm != null) leftLowerArm.localRotation = Quaternion.Euler(leftLowerArmEuler);
        if (leftHand != null) leftHand.localRotation = Quaternion.Euler(leftHandEuler);
    }
}
