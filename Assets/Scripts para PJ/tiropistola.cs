using UnityEngine;
using TMPro;
public class tiropistola : MonoBehaviour
{

    //bala da arma 
    public GameObject bala;

    //força da bala
    public float forçaTiro, forçaCima;
    //status de arma
    public float tempoEntreAtirar, Espalhar, tempoRecarga, tempoEntreTiros;

    public int tamanhoCarregador, tiroPorClick;

    public bool permetirSegurarBotão;

    int balasSobrando, balasAtiradas;

    //chamando input CHAMDNO INPUTSSSS!!!!

    private AcoesPlayer acoesPlayer;

    //bools

    bool atirando, preparadoParaAtirar, recarregando;

    //referencia

    public Camera fpsCam;
    public Transform pontoAtaque;

    //graficos
    public GameObject muzzleFlash;
    public TextMeshProUGUI ammunitionDisplay;

    //se houver bug
    public bool allowInvoke = true;

    private void Awake()
    {
        //ativando inputs
        acoesPlayer = new AcoesPlayer();
        //garantir que carregador esteja cheio
        balasSobrando = tamanhoCarregador;
        preparadoParaAtirar = true;
    }

    public void Update()
    {
        MyInput();
        //display da munição se está presente
        if (ammunitionDisplay != null)
            ammunitionDisplay.SetText(balasSobrando / tiroPorClick + " / " + tamanhoCarregador / tiroPorClick);
    }
    private void OnEnable()
    {
        //TROQUE O NOME FIO
        acoesPlayer.MovimentoPlayer.Atirar.Enable();
        acoesPlayer.MovimentoPlayer.Recarregar.Enable();
    }
    private void OnDisable()
    {
        //ESSE TBM ENTÃO FICA ESPERTO TROCO NO FUTURO
        acoesPlayer.MovimentoPlayer.Atirar.Disable();
        acoesPlayer.MovimentoPlayer.Recarregar.Disable();
    }
    private void MyInput()
    {
        //input TROCAR INPUT NO FUTURO
        //checar se permite segura botão e pegar o input certo
        if (permetirSegurarBotão && acoesPlayer.MovimentoPlayer.Atirar.IsPressed()) atirando = true;
        else atirando = false;

        //recarregar
        if (acoesPlayer.MovimentoPlayer.Recarregar.IsInProgress() && balasSobrando < tamanhoCarregador && !recarregando) Reload();


        //recarregar automaticamente ao atirar sem munição
        if (preparadoParaAtirar && atirando && !recarregando && balasSobrando <= 0) Reload();

        //atirando

        if (preparadoParaAtirar && atirando && !recarregando && balasSobrando > 0)
        {
            balasAtiradas = 0;
            atirar();
        }


    }
    private void atirar()
    {
        Debug.Log("Atirando");
        preparadoParaAtirar = false;

        //encontrar o ponto exato do acerto do raycast
        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));//meio da camera
        RaycastHit acerto;

        //checar se acertou alguma coisa
        Vector3 pontoAcerto;
        if (Physics.Raycast(ray, out acerto))
        {
            pontoAcerto = acerto.point;
        }
        else
        {
            pontoAcerto = ray.GetPoint(75);//apena o ponto longe do player
        }
        //calcular dreção do ponto de ataque pro ponto de mira
        Vector3 direçãoSemEspalhar = pontoAcerto - pontoAtaque.position;

        float x = Random.Range(-Espalhar, Espalhar);
        float y = Random.Range(-Espalhar, Espalhar);

        Vector3 direçãoComEspalhar = direçãoSemEspalhar + new Vector3(x, y, 0);

        GameObject balaAtual = Instantiate(bala, pontoAtaque.position, Quaternion.identity);//armazena as intancias dentro dele
                                                                                            //rotacionar a bala
        balaAtual.transform.forward = direçãoComEspalhar.normalized;

        //adicionar a força a bala
        balaAtual.GetComponent<Rigidbody>().AddForce(direçãoComEspalhar.normalized * forçaTiro, ForceMode.Impulse);
        balaAtual.GetComponent<Rigidbody>().AddForce(fpsCam.transform.up * forçaCima, ForceMode.Impulse);

        balasSobrando--;
        balasAtiradas++;
        if (allowInvoke)
        {
            Invoke("ResetShot", tempoEntreAtirar);
            allowInvoke = false;
        }

        if (balasAtiradas < tiroPorClick && balasSobrando > 0)
            Invoke("atirar", tempoEntreTiros);

    }
    private void ResetShot()
    {
        preparadoParaAtirar = true;
        allowInvoke = true;
    }
    private void Reload()
    {
        balasSobrando = tamanhoCarregador;
        recarregando = false;
    }
}