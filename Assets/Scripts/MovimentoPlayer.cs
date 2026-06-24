// biblioteca de acesso da unity
using UnityEngine;

// um classe chamada MovimentoPlayer
// : -> significao herança
// Monobehaviour - Classe de comportamento unico dentro da unity
[RequireComponent(typeof(CharacterController))]
public class MovimentodoPlayer : MonoBehaviour
{
    
    private CharacterController characterController;
    private AcoesPlayer acoesPlay;

    [SerializeField] private float velocidade;

    private void Awake()
    {
        acoesPlay = new AcoesPlayer();

    }

    // É chamado antes do primeiro frame de execução
    void Start()
    {
        
        characterController = GetComponent<CharacterController>();
       
    }
    private void OnEnable()
    {
        acoesPlay.MovimentoPlayer.Enable();
    }
    private void OnDisable()
    {
        acoesPlay.MovimentoPlayer.Disable();

    }

    // É chamado uma vez a cada frame.
    void Update()
    {
        Andar();
    }

    void Andar()
    { 
    
       Vector2 leituraInput = acoesPlay.MovimentoPlayer.Andar.ReadValue<Vector2>();

        Vector3 movimento = new Vector3(leituraInput.x, 0, leituraInput.y);

        characterController.Move(movimento * velocidade * Time.deltaTime);
    }




}
