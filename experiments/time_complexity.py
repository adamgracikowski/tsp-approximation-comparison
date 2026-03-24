import pandas as pd
import matplotlib.pyplot as plt
import argparse
import os

def plot_tsp_results(csv_filepath, output_dir):
    os.makedirs(output_dir, exist_ok=True)

    try:
        df = pd.read_csv(csv_filepath)
    except Exception as e:
        print(f"Error while reading CSV file: {e}")
        return

    required_cols = ['Algorithm', 'VertexCount', 'TotalElapsedMiliseconds']
    for col in required_cols:
        if col not in df.columns:
            print(f"Error: Column '{col}' is missing in the CSV file.")
            return

    plt.figure(figsize=(10, 6))

    grouped = df.groupby('Algorithm')

    for algorithm_name, group_data in grouped:
        group_data = group_data.sort_values(by='VertexCount')
        
        scatter = plt.scatter(
            group_data['VertexCount'], 
            group_data['TotalElapsedMiliseconds'], 
            label=algorithm_name, 
            alpha=0.8,
            s=50,
            zorder=3
        )
        
        base_color = scatter.get_facecolor()[0]
        
        plt.plot(
            group_data['VertexCount'], 
            group_data['TotalElapsedMiliseconds'], 
            color=base_color,
            alpha=0.3,   
            linewidth=2,
            zorder=2
        )

    plt.xlabel('Liczba wierzchołków', fontsize=12)
    plt.ylabel('Czas w milisekundach', fontsize=12)
    
    plt.legend(title='Algorytm', fontsize=10)
    plt.grid(True, linestyle='--', alpha=0.6, zorder=1)

    plt.tight_layout()

    plot_path = os.path.join(output_dir, "time_complexity_plot.png")
    plt.savefig(plot_path, dpi=300, bbox_inches='tight')

def main():
    parser = argparse.ArgumentParser(description="Time Complexity plotter for TSP algorithms based on CSV results.")
    parser.add_argument("-i", "--input", type=str, required=True, help="Path to the input CSV file containing TSP results.")
    parser.add_argument("--out", type=str, default="plots", help="Directory to save the generated plot (default: 'plots').")
    
    args = parser.parse_args()

    if not os.path.exists(args.input):
        print(f"Error: File '{args.input}' does not exist.")
        return

    plot_tsp_results(args.input, args.out)

if __name__ == "__main__":
    main()