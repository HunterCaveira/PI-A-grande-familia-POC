using System.Collections.Generic;
using UnityEngine;

public class HabilidadeCinto : MonoBehaviour, ISkills
{
    // ==========================================
    // 1. CONFIGURAÇÕES DA CÂMERA E ALCANCE
    // ==========================================

    [Header("Configurações da Câmera e Alcançe")]
    [SerializeField] private Camera cameraPrincipal;
    [SerializeField] private float distanciaMaxima = 15f;


    // ==========================================
    // 2. TEMPOS E VELOCIDADES
    // ==========================================

    [Header("Controle de Tempo e Velocidades")]
    [SerializeField] private float tempoCooldown = 3.0f;

    // Variáveis de tempo (mantendo compatibilidade com os dois nomes)
    private float tempoProximouso = 0f;
    public float TempoProximouso => tempoProximouso;

    // Compatibilidade com o nome antigo caso outro script chame:
    private float tempoProximoUso
    {
        get => tempoProximouso;
        set => tempoProximouso = value;
    }

    [SerializeField] private float velocidadeAvanco = 30f;
    [SerializeField] private float velocidadeRetorno = 20f;


    // ==========================================
    // 3. REFERÊNCIAS DE COMPONENTES E VISUAIS
    // ==========================================

    [Header("Controle De Referência e Visuais")]
    [SerializeField] private GerenciadorDeEquipamentos gerenciadorDeEquipamentos;
    [SerializeField] private GameObject prefabCinto;
    [SerializeField] private Transform pontoFivelaPontos;
    [SerializeField] private Transform pontoDestinoPuxao;


    // ==========================================
    // 4. CONFIGURAÇÃO DE SLOT E UI (INTERFACE)
    // ==========================================

    [Header("Configuração do Slot")]
    [SerializeField] private int slotIndex = 1; // 1 = Q, 2 = E, 3 = F
    public int SlotIndex => slotIndex;

    [Header("Imagens da UI")]
    [SerializeField] private Sprite fotoPronta;
    [SerializeField] private Sprite fotoRecarregando;
    public Sprite FotoPronta => fotoPronta;
    public Sprite FotoRecarregando => fotoRecarregando;


    // ==========================================
    // 5. COMBATE E MÁQUINA DE ESTADOS
    // ==========================================

    [Header("Filtragem de Time")]
    [SerializeField] private int idDoTime = 1;
    public int IdDoTime => idDoTime;

    private enum EstadoCinto { Inativo, Avancando, PuxandoInimigo, RecolhendoVazio }
    private EstadoCinto estadoAtual = EstadoCinto.Inativo;

    private Transform inimigoCapturado;
    private CharacterController controllerInimigo;
    private Rigidbody rbInimigo;

    private Vector3 posicaoInicialLocal;
    private Vector3 pontoFinalMundo;


    // ==========================================
    // 6. CICLO DE VIDA DA UNITY
    // ==========================================

    private void Awake()
    {
        gerenciadorDeEquipamentos = GetComponent<GerenciadorDeEquipamentos>();

        if (prefabCinto != null)
        {
            posicaoInicialLocal = prefabCinto.transform.localPosition;
        }
    }

    private void Update()
    {
        if (estadoAtual == EstadoCinto.Inativo) return;

        switch (estadoAtual)
        {
            case EstadoCinto.Avancando:
                ProcessarAvanco();
                break;

            case EstadoCinto.PuxandoInimigo:
                ProcessarPuxao();
                break;

            case EstadoCinto.RecolhendoVazio:
                ProcessarRecolhimento();
                break;
        }
    }


    // ==========================================
    // 7. LÓGICA DE DETECÇÃO, AVANÇO E PUXÃO
    // ==========================================

    private void ProcessarAvanco()
    {
        if (prefabCinto == null) return;

        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, pontoFinalMundo, velocidadeAvanco * Time.deltaTime);

        if (ChecarEPrenderInimigo()) return;

        if (Vector3.Distance(prefabCinto.transform.position, pontoFinalMundo) <= 0.2f)
        {
            estadoAtual = EstadoCinto.RecolhendoVazio;
        }
    }

    private void ProcessarRecolhimento()
    {
        if (prefabCinto == null) return;

        Vector3 destinoMao = pontoDestinoPuxao != null ? pontoDestinoPuxao.position : transform.position;

        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, destinoMao, velocidadeAvanco * Time.deltaTime);

        if (ChecarEPrenderInimigo()) return;

        if (Vector3.Distance(prefabCinto.transform.position, destinoMao) <= 0.2f)
        {
            FinalizarHabilidade();
        }
    }

    private bool ChecarEPrenderInimigo()
    {
        Vector3 posicaoCheck = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
        Collider[] hits = Physics.OverlapSphere(posicaoCheck, 0.6f);

        foreach (var col in hits)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            Sistemadevida vidaAlvo = col.GetComponent<Sistemadevida>();
            HabilidadeCinto scriptAlvo = col.GetComponent<HabilidadeCinto>();

            int timeAlvo = -1;
            if (vidaAlvo != null) timeAlvo = vidaAlvo.TimeID;
            else if (scriptAlvo != null) timeAlvo = scriptAlvo.IdDoTime;

            if (timeAlvo != -1 && timeAlvo != this.idDoTime)
            {
                inimigoCapturado = col.transform;

                controllerInimigo = col.GetComponent<CharacterController>();
                if (controllerInimigo != null) controllerInimigo.enabled = false;

                rbInimigo = col.GetComponent<Rigidbody>();
                if (rbInimigo != null) rbInimigo.isKinematic = true;

                estadoAtual = EstadoCinto.PuxandoInimigo;
                return true;
            }
        }

        return false;
    }

    private void ProcessarPuxao()
    {
        if (prefabCinto == null) return;

        Vector3 destinoMao = pontoDestinoPuxao != null ? pontoDestinoPuxao.position : transform.position;

        prefabCinto.transform.position = Vector3.MoveTowards(prefabCinto.transform.position, destinoMao, velocidadeRetorno * Time.deltaTime);

        if (inimigoCapturado != null)
        {
            Vector3 posicaoFivela = pontoFivelaPontos != null ? pontoFivelaPontos.position : prefabCinto.transform.position;
            inimigoCapturado.position = posicaoFivela;
        }

        if (Vector3.Distance(prefabCinto.transform.position, destinoMao) <= 0.8f)
        {
            FinalizarHabilidade();
        }
    }

    private void FinalizarHabilidade()
    {
        estadoAtual = EstadoCinto.Inativo;

        if (controllerInimigo != null)
        {
            controllerInimigo.enabled = true;
            controllerInimigo = null;
        }

        if (rbInimigo != null)
        {
            rbInimigo.isKinematic = false;
            rbInimigo = null;
        }

        inimigoCapturado = null;

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.transform.localRotation = Quaternion.identity;
        }

        CancelarHabilidade();

        if (gerenciadorDeEquipamentos != null)
        {
            gerenciadorDeEquipamentos.TrocarEquipamento(0);
        }
    }


    // ==========================================
    // 8. MÉTODOS DA INTERFACE (E COMPATIBILIDADE)
    // ==========================================

    public bool PodeUsar()
    {
        return Time.time >= tempoProximouso && estadoAtual == EstadoCinto.Inativo;
    }

    // Método antigo de compatibilidade caso outro script o chame:
    public bool EstaPronta()
    {
        return PodeUsar();
    }

    public void AtivarHabilidade()
    {
        if (!PodeUsar())
        {
            if (gerenciadorDeEquipamentos != null)
            {
                gerenciadorDeEquipamentos.TrocarEquipamento(0);
            }
            return;
        }

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.SetActive(true);
        }

        tempoProximouso = Time.time + tempoCooldown;

        Ray ray = cameraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        pontoFinalMundo = ray.origin + ray.direction * distanciaMaxima;

        if (Physics.Raycast(ray, out RaycastHit hit, distanciaMaxima))
        {
            pontoFinalMundo = hit.point;
        }

        if (prefabCinto != null)
        {
            prefabCinto.transform.LookAt(pontoFinalMundo);
        }

        estadoAtual = EstadoCinto.Avancando;
    }

    public void CancelarHabilidade()
    {
        estadoAtual = EstadoCinto.Inativo;

        if (controllerInimigo != null)
        {
            controllerInimigo.enabled = true;
            controllerInimigo = null;
        }

        if (rbInimigo != null)
        {
            rbInimigo.isKinematic = false;
            rbInimigo = null;
        }

        inimigoCapturado = null;

        if (prefabCinto != null)
        {
            prefabCinto.transform.localPosition = posicaoInicialLocal;
            prefabCinto.transform.localRotation = Quaternion.identity;
            prefabCinto.SetActive(false);
        }
    }
}