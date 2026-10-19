using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EstadoInimigo
    {
        Normal,
        Lento,
        ImobilizadoEIluminado
    }

    [Header("Estado Atual")]
    public EstadoInimigo estadoAtual = EstadoInimigo.Normal;

    // Script de movimento obtido automaticamente via GetComponent
    private MonoBehaviour scriptDeMovimento;

    [Header("Componentes de Física")]
    private CharacterController characterController;

    [Header("Configurações Visuais (X-Ray / Brilho através da parede)")]
    [SerializeField] private Renderer[] renderersDoInimigo; // Arraste os meshes do inimigo aqui
    [SerializeField] private Material materialXRay;         // Material especial com ZTest Always para ver pelas paredes
    private Material[] materiaisOriginais;

    [Header("Controle de Modificadores")]
    [HideInInspector] public float multiplicadorVelocidade = 1f; // Usado pelo script de movimento para aplicar a lentidão

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Pega automaticamente o primeiro outro script do GameObject que não seja este EnemyController
        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
        {
            if (script != this)
            {
                scriptDeMovimento = script;
                break;
            }
        }

        // Salva os materiais originais de cada parte do inimigo
        if (renderersDoInimigo != null && renderersDoInimigo.Length > 0)
        {
            materiaisOriginais = new Material[renderersDoInimigo.Length];
            for (int i = 0; i < renderersDoInimigo.Length; i++)
            {
                if (renderersDoInimigo[i] != null)
                {
                    materiaisOriginais[i] = renderersDoInimigo[i].material;
                }
            }
        }
    }

    void Update()
    {
        // Se o inimigo estiver preso pela Skill 2, ele para de executar o movimento
        if (scriptDeMovimento != null && !scriptDeMovimento.enabled && estadoAtual == EstadoInimigo.ImobilizadoEIluminado)
        {
            return;
        }

        // --- SEU CÓDIGO DE MOVIMENTO NORMAL FICA AQUI ---
    }

    // ==========================================
    // 1. SISTEMA DE EMPURRÃO (SKILL 1)
    // ==========================================

    public void ReceberEmpurrao(Vector3 direcao, float forca, float duracao)
    {
        // Só toma empurrão se não estiver totalmente imobilizado
        if (estadoAtual != EstadoInimigo.ImobilizadoEIluminado && characterController != null)
        {
            StartCoroutine(AplicarEmpurraoRoutine(direcao, forca, duracao));
        }
    }

    private IEnumerator AplicarEmpurraoRoutine(Vector3 direcao, float forca, float duracao)
    {
        float tempoDecorrido = 0f;

        while (tempoDecorrido < duracao && estadoAtual != EstadoInimigo.ImobilizadoEIluminado)
        {
            float fatorForca = Mathf.Lerp(forca, 0f, tempoDecorrido / duracao);
            characterController.Move(direcao * fatorForca * Time.deltaTime);

            tempoDecorrido += Time.deltaTime;
            yield return null;
        }
    }

    // ==========================================
    // 2. SISTEMA DE LENTIDÃO (PROJÉTIL)
    // ==========================================

    public void AplicarLentidao(float porcentagemReducao, float duracao)
    {
        // Se já estiver preso/imobilizado, ignora a lentidão para não sobrescrever o efeito maior
        if (estadoAtual == EstadoInimigo.ImobilizadoEIluminado) return;

        StartCoroutine(EfeitoLentidaoRoutine(porcentagemReducao, duracao));
    }

    private IEnumerator EfeitoLentidaoRoutine(float porcentagemReducao, float duracao)
    {
        // Muda o estado para Lento
        estadoAtual = EstadoInimigo.Lento;

        // Define o multiplicador (Ex: 0.5f reduz a velocidade em 50%)
        multiplicadorVelocidade = 1f - porcentagemReducao;
        Debug.Log($"{gameObject.name} ficou lento! Multiplicador de velocidade: {multiplicadorVelocidade}");

        // Aguarda o tempo da duração da lentidão
        yield return new WaitForSeconds(duracao);

        // Se ele não tiver sido imobilizado nesse meio tempo, restaura a velocidade e o estado normal
        if (estadoAtual == EstadoInimigo.Lento)
        {
            multiplicadorVelocidade = 1f;
            estadoAtual = EstadoInimigo.Normal;
            Debug.Log($"{gameObject.name} recuperou a velocidade normal.");
        }
    }

    // ==========================================
    // 3. SISTEMA DE PRISÃO E VISÃO X-RAY (SKILL 2)
    // ==========================================

    public void AplicarEfeitoPrisao(float duracao)
    {
        StartCoroutine(EfeitoPrisaoRoutine(duracao));
    }

    private IEnumerator EfeitoPrisaoRoutine(float duracao)
    {
        // 1. Muda o estado para imobilizado
        estadoAtual = EstadoInimigo.ImobilizadoEIluminado;
        multiplicadorVelocidade = 0f; // Zera o multiplicador

        // 2. DESATIVA O SCRIPT DE MOVIMENTO OBTIDO VIA GETCOMPONENT IMEDIATAMENTE
        if (scriptDeMovimento != null)
        {
            scriptDeMovimento.enabled = false;
        }

        // 3. Ativa o visual X-Ray (brilha e atravessa paredes)
        AtivarEfeitoXRay(true);

        // 4. Aguarda o tempo da zona de comando
        yield return new WaitForSeconds(duracao);

        // 5. Restaura tudo ao normal (Reativa o script de movimento)
        AtivarEfeitoXRay(false);

        if (scriptDeMovimento != null)
        {
            scriptDeMovimento.enabled = true;
        }

        multiplicadorVelocidade = 1f;
        estadoAtual = EstadoInimigo.Normal;

        Debug.Log($"{gameObject.name} recuperou os movimentos e saiu do X-Ray!");
    }

    // ==========================================
    // 4. MÉTODOS AUXILIARES E DE CONSULTA
    // ==========================================

    public bool EstaImobilizado()
    {
        return estadoAtual == EstadoInimigo.ImobilizadoEIluminado;
    }

    public bool EstaLento()
    {
        return estadoAtual == EstadoInimigo.Lento;
    }

    private void AtivarEfeitoXRay(bool ativar)
    {
        if (renderersDoInimigo == null) return;

        for (int i = 0; i < renderersDoInimigo.Length; i++)
        {
            if (renderersDoInimigo[i] != null)
            {
                if (ativar && materialXRay != null)
                {
                    renderersDoInimigo[i].material = materialXRay;
                }
                else if (!ativar && materiaisOriginais[i] != null)
                {
                    renderersDoInimigo[i].material = materiaisOriginais[i];
                }
            }
        }
    }
}