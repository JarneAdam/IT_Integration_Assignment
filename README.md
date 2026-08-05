# Serverless Data Pipeline - Market & Weather Update

## Projectbeschrijving
Dit project is een volledig serverless integratie-oplossing gebouwd op Microsoft Azure. Het automatiseert het dagelijks ophalen van actuele wisselkoersen (Forex) en weersinformatie (Open-Meteo). De data wordt verwerkt, als backup weggeschreven naar Blob Storage, en via een Service Bus Queue doorgestuurd naar een Logic App die het als een netjes opgemaakte e-mail aflevert.

## Architectuur

Het project volgt het "Separation of Concerns" principe: het ophalen van de data (Azure Function) is strikt gescheiden van het verzenden van de notificatie (Logic App). Ze communiceren asynchroon met elkaar via Azure Service Bus.

```mermaid
graph TD
    %% Styling
    classDef azureFunction fill:#fa8c16,stroke:#fff,stroke-width:2px,color:#fff;
    classDef logicApp fill:#1890ff,stroke:#fff,stroke-width:2px,color:#fff;
    classDef storage fill:#52c41a,stroke:#fff,stroke-width:2px,color:#fff;
    classDef serviceBus fill:#eb2f96,stroke:#fff,stroke-width:2px,color:#fff;
    classDef external fill:#8c8c8c,stroke:#fff,stroke-width:2px,color:#fff;

    %% Nodes
    Timer((Timer Trigger<br/>08:00 AM))
    API1[Forex API<br/>Frankfurter]:::external
    API2[Weather API<br/>Open-Meteo]:::external
    
    Func[Azure Function<br/>C# .NET 8 Isolated]:::azureFunction
    
    Blob[(Azure Blob Storage<br/>Raw JSON Backup)]:::storage
    SBQueue{Service Bus Queue<br/>'sbq-messages'}:::serviceBus
    
    LogicApp[Azure Logic App<br/>Parse JSON & Send]:::logicApp
    Email((Office 365<br/>E-mail))
    
    %% Relaties
    Timer -->|Triggers| Func
    Func -->|Fetch USD Rate| API1
    Func -->|Fetch Temp/Wind| API2
    API1 -.-> Func
    API2 -.-> Func
    
    Func -->|Saves combined JSON| Blob
    Func -->|Sends Base64 JSON Payload| SBQueue
    
    SBQueue -->|Triggers when message arrives| LogicApp
    LogicApp -->|Decodes & Formats| Email