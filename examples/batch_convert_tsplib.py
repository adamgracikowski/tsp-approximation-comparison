import argparse
import math
import os
import glob

def load_optimal_values(filepath):
    optimal_dict = {}
    with open(filepath, 'r', encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if not line or ':' not in line:
                continue
            
            parts = line.split(':')
            name = parts[0].strip()
            cost = int(parts[1].strip())
            optimal_dict[name] = cost
            
    return optimal_dict

def calculate_euc_2d_distance(p1, p2):
    xd = p1[0] - p2[0]
    yd = p1[1] - p2[1]
    return int(math.floor(math.sqrt(xd*xd + yd*yd) + 0.5))

def convert_single_file(input_path, optimal_cost, output_dir):
    with open(input_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    dimension = 0
    reading_nodes = False
    nodes = {}

    for line in lines:
        line = line.strip()
        if not line or line == "EOF":
            break
        
        if line.startswith("DIMENSION"):
            parts = line.split(':')
            if len(parts) > 1:
                dimension = int(parts[1].strip())
            continue
            
        if line == "NODE_COORD_SECTION":
            reading_nodes = True
            continue
            
        if reading_nodes:
            parts = line.split()
            if len(parts) >= 3:
                node_id = int(parts[0])
                x = float(parts[1])
                y = float(parts[2])
                nodes[node_id] = (x, y)

    if dimension == 0 or len(nodes) != dimension:
        raise ValueError(f"Expected {dimension} vertices, but found {len(nodes)}.")

    matrix = [[0] * dimension for _ in range(dimension)]
    for i in range(1, dimension + 1):
        for j in range(1, dimension + 1):
            if i != j:
                matrix[i-1][j-1] = calculate_euc_2d_distance(nodes[i], nodes[j])

    for k in range(dimension):
        for i in range(dimension):
            for j in range(dimension):
                if i != j and j != k and i != k:
                    if matrix[i][j] > matrix[i][k] + matrix[k][j]:
                        matrix[i][j] = matrix[i][k] + matrix[k][j]

    output_filename = f"n{dimension}o{optimal_cost}.txt"
    output_path = os.path.join(output_dir, output_filename)
    original_filename = os.path.basename(input_path)

    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(f"# {original_filename}\n")
        f.write(f"{dimension}\n")
        for row in matrix:
            f.write(" ".join(map(str, row)) + "\n")

    return output_filename

def main():
    parser = argparse.ArgumentParser(description="Batch converter for TSPLIB to custom format with optimal costs.")
    parser.add_argument("-i", "--input", type=str, required=True, help="Directory containing original .txt files.")
    parser.add_argument("-opt", "--optimal-file", type=str, required=True, help="Text file with optimal values mapping.")
    parser.add_argument("-o", "--output", type=str, default="tsplib_examples", help="Output directory for converted files.")

    args = parser.parse_args()

    if not os.path.exists(args.output):
        os.makedirs(args.output)

    print("Loading optimal values mapping...")
    try:
        optimal_dict = load_optimal_values(args.optimal_file)
        print(f"Loaded {len(optimal_dict)} mappings.\n")
    except Exception as e:
        print(f"Error loading optimal values file: {e}")
        return

    search_pattern = os.path.join(args.input, "*.txt")
    tsp_files = glob.glob(search_pattern)

    if not tsp_files:
        print(f"No .txt files found in '{args.input}'.")
        return

    print(f"Found {len(tsp_files)} .txt files to process. Starting conversion...\n")
    
    success_count = 0
    skip_count = 0

    for filepath in tsp_files:
        filename = os.path.basename(filepath)
        base_name = os.path.splitext(filename)[0]

        if base_name not in optimal_dict:
            print(f"[SKIPPED] {filename} - No optimal value found in mapping file.")
            skip_count += 1
            continue

        optimal_cost = optimal_dict[base_name]
        
        try:
            out_name = convert_single_file(filepath, optimal_cost, args.output)
            print(f"[OK] Converted {filename} -> {out_name}")
            success_count += 1
        except Exception as e:
            print(f"[ERROR] Failed to convert {filename}: {e}")

    print("\n--- Summary ---")
    print(f"Successfully converted: {success_count}")
    print(f"Skipped (missing optimal value): {skip_count}")

if __name__ == "__main__":
    main()