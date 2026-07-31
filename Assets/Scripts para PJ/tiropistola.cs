using UnityEngine;

using TMPro;

public class tiropistola : MonoBehaviour

{

    //bala da arma 

    public GameObject bala;

    //força da bala

    public float forçaTiro, forçaCima;

    //status de arma (Removido o tempoEntreTiros, não será mais usado)

    public float tempoEntreAtirar, Espalhar, tempoRecarga;

    public int tamanhoCarregador, tiroPorClick;

    public bool permetirSegurarBotão;

    int balasSobrando;

    //chamando input

    private AcoesPlayer acoesPlayer;

    //bools

    bool atirando, preparadoParaAtirar, recarregando;

    //referencia

    public Camera fpsCam;

    public Transform pontoAtaque;

    //graficos

    public GameObject muzzleFlash;

    public TextMeshProUGUI ammunitionDisplay;

    private void Awake()

    {

        acoesPlayer = new AcoesPlayer();

        balasSobrando = tamanhoCarregador;

        preparadoParaAtirar = true;

    }

    public void Update()

    {

        MyInput();

        if (ammunitionDisplay != null)

            ammunitionDisplay.SetText(balasSobrando / tiroPorClick + " / " + tamanhoCarregador / tiroPorClick);

    }

    private void OnEnable()

    {

        acoesPlayer.MovimentoPlayer.Atirar.Enable();

        acoesPlayer.MovimentoPlayer.Recarregar.Enable();

    }

    private void OnDisable()

    {

        acoesPlayer.MovimentoPlayer.Atirar.Disable();

        acoesPlayer.MovimentoPlayer.Recarregar.Disable();

    }

    private void MyInput()

    {

        // 1. LÓGICA DE TIRO (Individual vs Automático)

        if (permetirSegurarBotão)

        {

            atirando = acoesPlayer.MovimentoPlayer.Atirar.IsPressed();

        }

        else

        {

            atirando = acoesPlayer.MovimentoPlayer.Atirar.WasPressedThisFrame();

        }

        // 2. LÓGICA DE RECARGA

        if (acoesPlayer.MovimentoPlayer.Recarregar.WasPressedThisFrame() && balasSobrando < tamanhoCarregador && !recarregando)

            Reload();

        if (preparadoParaAtirar && atirando && !recarregando && balasSobrando <= 0)

            Reload();

        // 3. INÍCIO DO TIRO

        if (preparadoParaAtirar && atirando && !recarregando && balasSobrando > 0)

        {

            atirar();

        }

    }

    private void atirar()

    {

        preparadoParaAtirar = false;

        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        RaycastHit acerto;

        Vector3 pontoAcerto;

        if (Physics.Raycast(ray, out acerto))

        {

            pontoAcerto = acerto.point;

        }

        else

        {

            pontoAcerto = ray.GetPoint(75);

        }

        Vector3 direçãoSemEspalhar = pontoAcerto - pontoAtaque.position;

        // A MÁGICA ACONTECE AQUI: O laço 'for' atira todas as balas do clique no mesmo milissegundo!

        for (int i = 0; i < tiroPorClick; i++)

        {

            // Proteção extra: se acabar a bala no meio do jato, ele interrompe o laço

            if (balasSobrando <= 0) break;

            // Calcula um espalhamento diferente para CADA bala do jato

            float x = Random.Range(-Espalhar, Espalhar);

            float y = Random.Range(-Espalhar, Espalhar);

            Vector3 direçãoComEspalhar = direçãoSemEspalhar + new Vector3(x, y, 0);

            // Cria a bala

            GameObject balaAtual = Instantiate(bala, pontoAtaque.position, Quaternion.identity);

            balaAtual.transform.forward = direçãoComEspalhar.normalized;

            // Empurra a bala

            balaAtual.GetComponent<Rigidbody>().AddForce(direçãoComEspalhar.normalized * forçaTiro, ForceMode.Impulse);

            balaAtual.GetComponent<Rigidbody>().AddForce(fpsCam.transform.up * forçaCima, ForceMode.Impulse);

            // Gasta 1 de munição para cada projétil criado

            balasSobrando--;

        }

        // Depois que todo o jato saiu de uma vez, iniciamos o tempo de espera (cooldown) para o próximo clique

        Invoke("ResetShot", tempoEntreAtirar);

    }

    private void ResetShot()

    {

        preparadoParaAtirar = true;

    }

    private void Reload()

    {

        recarregando = true;

        Invoke("TerminarRecarga", tempoRecarga);

    }

    private void TerminarRecarga()

    {

        balasSobrando = tamanhoCarregador;

        recarregando = false;

    }

}
