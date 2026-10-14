# FireGuard360 Project Description

## Legacy WinForms Edition

The solution now includes `FireGuard360.Legacy.WinForms`, a Windows desktop edition that demonstrates how the same wildfire-management business workflow can operate in a traditional legacy application. The original Azure Functions implementation remains unchanged for architectural comparison.

The desktop workflow preserves these business stages:

1. Validate and collect environmental sensor readings.
2. Calculate a wildfire risk score and severity level.
3. Project a geographic fire-spread path.
4. identify affected regional infrastructure.
5. Recommend monitoring, notification, resource, and evacuation actions.
6. Generate an executive incident summary.
7. Prepare multilingual report records.

In the legacy edition, in-process service calls replace Azure Service Bus triggers and local JSON records replace Cosmos DB. This makes the application suitable for disconnected Windows workstations while retaining the original domain workflow and models.

### Run the legacy desktop application

Requirements: Windows 10/11, Visual Studio 2022, and the .NET 8 SDK.

```powershell
cd WildfireMonitor
dotnet run --project FireGuard360.Legacy.WinForms
```

The application stores records under `%LOCALAPPDATA%\FireGuard360Legacy\wildfire-records.json` and can export a text-based incident report from the operations screen.

## Overview
FireGuard360 is a modular, agent-based application developed in C# (.NET 8) designed for monitoring, predicting, and responding to wildfires. It leverages a distributed architecture where specialized agents handle distinct tasks within the wildfire data processing pipeline. The system integrates with external services to enhance its capabilities, ensuring scalability, security, and real-time processing.

## Main Components

### Functional Agents
Each agent is responsible for a specific task in the wildfire management workflow:
- **DataCollectorAgent**: Gathers data from sensors and external APIs, providing real-time environmental and situational inputs.
- **FireRiskPredictorAgent**: Analyzes collected data to predict areas at risk of wildfires using advanced analytics.
- **FireSpreadSimulatorAgent**: Simulates potential fire spread based on mathematical models, factoring in terrain, weather, and fuel conditions.
- **ImpactAssessmentAgent**: Evaluates the potential impact of wildfires on infrastructure, ecosystems, and communities.
- **ResponseAdvisorAgent**: Generates actionable response and mitigation strategies based on predictive and impact analyses.
- **SummarizerAgent**: Produces concise executive summaries of data and analyses for stakeholders.
- **TranslatorAgent**: Translates summaries and reports into multiple languages to support international collaboration.

### Data Models
The system uses structured data models to facilitate information exchange between agents:
**SummaryData**, **TranslationData**, **ImpactModel**, **RiskData**, **SensorData**, **ResponseData**, **InfrastructureData**, **ImpactData**, **GeoLocation**, **FireSpreadModel** and **SpreadData**.

### Services
WildfireAgents integrates with Azure Services:
- **AzureMapsService**: Provides geolocation and mapping capabilities for spatial analysis and visualization.
- **CosmosDbService**: Stores data in a scalable NoSQL database for efficient retrieval and querying.
- **KeyVaultService**: Manages secrets and keys securely to ensure system integrity.
- **OpenAIService**: Leverages natural language processing for enhanced data interpretation and reporting.
- **ServiceBusDispatcher**: Enables asynchronous communication between agents for seamless data flow.
- **TranslatorService**: Supports automatic text translation to make outputs accessible in multiple languages.

## Workflow
The application follows a streamlined workflow to address wildfire challenges:
1. **Data Collection**: The `DataCollectorAgent` retrieves real-time data from sensors and external APIs.
2. **Risk Prediction**: The `FireRiskPredictorAgent` processes data to identify high-risk wildfire areas.
3. **Spread Simulation**: The `FireSpreadSimulatorAgent` models potential fire spread based on environmental factors.
4. **Impact Assessment**: The `ImpactAssessmentAgent` calculates potential damages to infrastructure and ecosystems.
5. **Response Recommendations**: The `ResponseAdvisorAgent` provides actionable strategies for mitigation and response.
6. **Summary Generation**: The `SummarizerAgent` creates concise summaries for decision-makers.
7. **Translation**: The `TranslatorAgent` translates summaries into required languages for global accessibility.
8. **Storage and Communication**: Data and analyses are stored in Cosmos DB and shared via Service Bus for real-time updates.

## Responsible AI in WildfireAgents
- **Transparency**
Specialized agents (Summarizer, Predictor, Translator) with documented roles.
Data stored in auditable models (SummaryData, RiskData, etc.).

- **Explainability**
Predictions and analyses come with understandable summaries and translations.
Generative AI is used to explain and contextualize the results.

- **Privacy & Security**
Sensitive data protected via KeyVault and CosmosDb.
Access is controlled and monitored to prevent misuse.

- **Fairness & Non-Discrimination**
Automatic translations ensure equal access to information.

- **Accountability**
Automated decisions are reviewable by humans.
Detailed logs and continuous reviews ensure reliability.

- **Safety & Robustness**
Resilient system with fallback and continuous monitoring.
Designed to withstand AI or infrastructure failures.

## Technical Details
- **Framework**: Built on .NET 8 for robust performance and cross-platform compatibility.
- **Architecture**: Agent-based, modular design to ensure scalability and maintainability.
- **External Integrations**: Leverages Azure services (Maps, Cosmos DB, Key Vault, Service Bus) and OpenAI for advanced functionality.
- **Communication**: Asynchronous messaging via Service Bus ensures efficient agent coordination.

## Core Features
- Modular Architecture: Specialized agents for data collection, prediction, simulation, assessment, response, 
summarization, and translation ensure scalability.
- Real-Time Monitoring: Continuous sensor and API data ingestion for near real-time updates.
- Risk Prediction, Fire Spread Simulation and Impact analysis for infrastructure and ecosystems.
- Natural Language Summarization: AI-generated 
summaries for non-technical stakeholders.
- Multilingual Support.

## Purpose
WildfireAgents aims to provide a comprehensive solution for wildfire management by combining real-time data collection, predictive analytics, impact assessment, and actionable response strategies. Its modular architecture, structured data models, and external integrations make it adaptable to various environmental and operational contexts, supporting stakeholders in mitigating wildfire risks effectively.
